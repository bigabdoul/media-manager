using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arts.Entity.Data;
using Arts.Entity.Models;
using Arts.Web.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DiskFile = System.IO.File;

namespace Arts.Web.Controllers
{
    public class ProjectControllerBase : Controller
    {
        protected const long MAX_SIZE_IMAGE = 0x300000L; // 3MB
        protected const long MAX_SIZE_AUDIO = 0x1400000L; // 20MB
        protected const long MAX_SIZE_VIDEO = 0x3200000L; // 50MB
        protected const string MEDIA_ROOT = "/uploads";

        protected ApplicationUser AppUser;
        protected readonly string MediaPath;
        protected readonly IHostingEnvironment HostingEnvironment;

        /// <summary>
        /// The application's database context.
        /// </summary>
        protected readonly ApplicationDbContext Database;
        
        /// <summary>
        /// An array containing regular expressions of all characters that are not allowed in file names.
        /// </summary>
        public static readonly string[] InvalidFileNameCharsRegEx = 
            Path.GetInvalidFileNameChars().Select(c => $@"\{c}").ToArray();

        /// <summary>
        /// A string representing a regular expression used to exclude all characters that are not allowed in file names.
        /// </summary>
        public static readonly string InvalidFileNameRegEx = string.Join("", InvalidFileNameCharsRegEx);

        public ProjectControllerBase(ApplicationDbContext context, IHostingEnvironment environment)
        {
            Database = context;
            HostingEnvironment = environment;
            MediaPath = Path.Combine(environment.WebRootPath, MEDIA_ROOT.TrimStart('/'));
        }

        protected ApplicationUser GetUser()
        {
            if (Database == null) return null;
            return AppUser ?? (AppUser = Database.Users.Where(u => u.UserName == User.Identity.Name).SingleOrDefault());
        }

        protected string GetUserId()
        {
            if (Database == null) return null;
            return (AppUser ?? (AppUser = Database.Users.Where(u => u.UserName == User.Identity.Name).SingleOrDefault()))?.Id;
        }

        protected async Task<string> GetUserIdAsync()
        {
            if (Database == null) return null;
            return (AppUser ?? (AppUser = await Database.Users.Where(u => u.UserName == User.Identity.Name).SingleOrDefaultAsync()))?.Id;
        }

        #region File Management

        public async Task<IActionResult> MediaDelete(long id)
        {
            var count = await DeleteMediaFile(id);
            return Json(new { success = count == 0 || count == 1 });
        }

        /// <summary>
        /// Attempts to delete a media item with the specified <paramref name="id"/>.
        /// The operation fails if the item has more than 1 active references.
        /// </summary>
        /// <param name="id">The identifier of the media item to delete.</param>
        /// <returns>
        /// A long integer the represents the number of active references. 
        /// 0 or 1 means the operation was a success.
        /// -1 means the media file does not exist in the data store.
        /// </returns>
        protected async Task<long> DeleteMediaFile(long id)
        {
            var refCount = -1L;

            if (MediaFileExists(id, out var media, await GetUserIdAsync()))
            {
                // check if the media is referenced by other components
                refCount = await CountMediaReferencesAsync(id);

                if (refCount <= 1)
                {
                    // remove the media from the database
                    Database.Remove(media);
                    var success = await Database.SaveChangesAsync() > 0;
                    success |= await TryDeletePhysicalFile(media.MediaUrl);
                }
            }

            return refCount;
        }

        /// <summary>
        /// Counts the number of albums and album items that reference the media identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The media file identifier.</param>
        /// <returns></returns>
        protected async Task<long> CountMediaReferencesAsync(long id)
        {
            var refCount = await Database.MediaAlbums.CountAsync(a => a.MediaFileId == id);
            refCount += await Database.MediaAlbumItems.CountAsync(a => a.MediaFileId == id);
            return refCount;
        }

        /// <summary>
        /// Attempts to delete the given file.
        /// </summary>
        /// <param name="virtualPath">The virtual path to the file to delete.</param>
        /// <param name="maxRetries">The maximum number to retry deleting the file should an I/O exception be raised.</param>
        /// <returns>true if the file was deleted; otherwise, false.</returns>
        protected async Task<bool> TryDeletePhysicalFile(string virtualPath, int maxRetries = 10)
        {
            var tries = 1;
            var success = false;

            if (maxRetries < 0) maxRetries = 0;

            while (true)
            {
                try
                {
                    if (PhysicalFileExists(virtualPath))
                    {
                        var path = HostingEnvironment.WebRootPath + virtualPath;
                        DiskFile.Delete(path);
                        success = true;
                    }
                    break;
                }
                catch(IOException ex)
                {
                    Debug.WriteLine(ex);
                    if (++tries > maxRetries) break;
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    break;
                }
            }

            return success;
        }

        /// <summary>
        /// Checks whether the virtual file path exists on the disk under the application's web root path.
        /// </summary>
        /// <param name="virtualPath">The virtual path to check.</param>
        /// <returns></returns>
        protected bool PhysicalFileExists(string virtualPath)
        {
            var path = HostingEnvironment.WebRootPath + virtualPath;
            return DiskFile.Exists(path);
        }

        /// <summary>
        /// Checks whether a <see cref="MediaFile"/> with the given hash code, 
        /// and optionally belonging to the specified user, exists in the database.
        /// </summary>
        /// <param name="hashCode">The hash code to check.</param>
        /// <param name="media">Returns the media file, if any.</param>
        /// <param name="userId">The user identifier to include in the query.</param>
        /// <returns></returns>
        protected bool MediaFileExists(string hashCode, out MediaFile media, string userId = null)
        {
            IQueryable<MediaFile> query = Database.MediaFiles.Where(f => f.HashCode == hashCode);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(f => f.UserId == userId);
            }

            media = query.FirstOrDefault();
            return media != null;
        }

        /// <summary>
        /// Checks whether a <see cref="MediaFile"/> with the given <paramref name="id"/>, 
        /// and optionally belonging to the specified user, exists in the database.
        /// </summary>
        /// <param name="id">The identifier of the media to check.</param>
        /// <param name="media">Returns the media file, if any.</param>
        /// <param name="userId">The user identifier to include in the query.</param>
        /// <returns></returns>
        protected bool MediaFileExists(long? id, out MediaFile media, string userId = null)
        {
            media = null;
            if (id == null) return false;

            IQueryable<MediaFile> query = Database.MediaFiles.Where(f => f.Id == id);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(f => f.UserId == userId);
            }

            media = query.FirstOrDefault();
            return media != null;
        }

        protected IQueryable<MediaAlbumItem> GetMediaItems(MediaType type)
        {
            return Database.MediaAlbumItems.Where(s => s.MediaType == type);
        }

        /// <summary>
        /// Checks whether a non-zero length file with the specified name has been uploaded along with the current request.
        /// </summary>
        /// <param name="name">The name of the file to check.</param>
        /// <returns></returns>
        protected bool HasUploadedFile(string name) => Request.Form.Files.GetFile(name)?.Length > 0L;

        /// <summary>
        /// Checks whether any non-zero length file with the specified name has been uploaded along with the current request.
        /// </summary>
        /// <param name="name">The name of the file to check.</param>
        /// <returns></returns>
        protected bool HasAnyUploadedFile(string name) 
            => Request.Form.Files.GetFiles(name)?.Any(f => f.Length > 0L) == true;

        /// <summary>
        /// Returns an array of all non-zero length files (of the specified name) uploaded along with the current request.
        /// </summary>
        /// <param name="name">
        /// The name of the uploaded files to retrieve; if null, all non-zero length files are returned.
        /// </param>
        /// <returns></returns>
        protected IFormFile[] GetUploadedFiles(string name = null)
        {
            IFormFile[] files = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                files = Request.Form.Files.Where(f => f.Length > 0L).ToArray();
            }
            else
            {
                var file = Request.Form.Files.GetFile(name);
                if (file?.Length > 0L)
                {
                    files = new[] { file };
                }
            }

            return files;
        }

        /// <summary>
        /// Creates a <see cref="MediaFile"/> in memory (for database storage), or saves it to the disk.
        /// </summary>
        /// <param name="name">
        /// The name of the file to save. If null, only the first non-zero length file will be created or saved.
        /// </param>
        /// <param name="saveToDisk">true to save the file to the disk; false to create in memory.</param>
        /// <param name="userId">The identifier of the user whom the file belongs to.</param>
        /// <returns></returns>
        protected async Task<MediaFile> CreateFileAsync(string name = null, bool saveToDisk = false, string userId = null)
        {
            return (await CreateAllFilesAsync(name, saveToDisk, userId))?.FirstOrDefault();
        }

        /// <summary>
        /// Creates an array of <see cref="MediaFile"/> in memory (for database storage), or saves them to the disk.
        /// </summary>
        /// <param name="name">The name of the file to save. If null, all files will be saved.</param>
        /// <param name="saveToDisk"></param>
        /// <param name="userId">The identifier of the user whom the files belong to.</param>
        /// <returns></returns>
        [Authorize]
        protected async Task<MediaFile[]> CreateAllFilesAsync(string name = null, bool saveToDisk = false, string userId = null)
        {
            if (saveToDisk)
            {
                return await SaveAllFilesAsync(name, userId);
            }

            var files = GetUploadedFiles(name);

            if (files?.Length > 0L)
            {
                var index = 0;
                var array = new MediaFile[files.Length];
                
                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = await GetUserIdAsync();
                }

                foreach (var file in files)
                {
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        var content = ms.ToArray();

                        array[index++] = new MediaFile
                        {
                            UserId = userId,
                            HasContent = true,
                            Content = content,
                            ContentType = file.ContentType,
                            FileName = file.FileName,
                            DateCreated = DateTime.Now,
                            Length = file.Length,
                            HashCode = CryptoExtensions.ComputeHash(content)
                        };
                    }
                }

                return array;
            }

            return null;
        }

        /// <summary>
        /// Saves and returns the first non-zero length file identified by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the file to save. If null, only the first non-zero length file will be saved.</param>
        /// <param name="userId">The identifier of the user whom the file belongs to.</param>
        /// <returns>An initialized <see cref="MediaFile"/>, or null.</returns>
        protected async Task<MediaFile> SaveFileAsync(string name = null, string userId = null)
        {
            return (await SaveAllFilesAsync(name, userId))?.FirstOrDefault();
        }

        /// <summary>
        /// Saves one or all uploaded non-zero length files to the disk in a subdirectory under the <see cref="MediaPath"/>.
        /// </summary>
        /// <param name="name">The name of the file to save. If null, all files will be saved.</param>
        /// <param name="userId">The identifier of the user whom the files belong to.</param>
        /// <returns>An array of <see cref="MediaFile"/> objects, or null.</returns>
        /// <remarks>
        /// Files are saved organized in subfolders named after the current year and month.
        /// The file names are optimized for URLs by replacing weired and/or illegal characters with hyphens.
        /// </remarks>
        [Authorize]
        protected async Task<MediaFile[]> SaveAllFilesAsync(string name = null, string userId = null)
        {
            var files = GetUploadedFiles(name);

            if (files?.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = await GetUserIdAsync();
                }

                string yearMonth = null;
                var dir = CreateSubDirectory(ref yearMonth, userId, MediaPath);

                var index = 0;
                var array = new MediaFile[files.Length];

                foreach (var file in files)
                {
                    using (var fStream = file.OpenReadStream())
                    {
                        string ext = null;
                        var fileName = SanitizeInput(file.FileName, true);

                        ExtractFileNameParts(fileName, ref name, ref ext);

                        var sanitized = SanitizeInput($"{name.ToLowerInvariant()}-{Guid.NewGuid().ToString("n")}{ext}");
                        var path = Path.Combine(dir, sanitized);

                        using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                        {
                            await fStream.CopyToAsync(fs);
                            sanitized = $"{MEDIA_ROOT}/{yearMonth}/{sanitized}".Replace('\\', '/');

                            array[index++] = new MediaFile
                            {
                                HasContent = false, // the content is not stored in the database
                                UserId = userId,
                                ContentType = file.ContentType,
                                DateCreated = DateTime.Now,
                                FileName = fileName,
                                MediaUrl = sanitized,
                                Length = file.Length,
                                HashCode = fs.ComputeHash()
                            };

                            //if (true == file.ContentType?.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    await CreateThumbnailImagesAsync(fs, path, file.ContentType);
                            //}
                        }
                    }
                }

                return array;
            }

            return null;
        }

        protected async Task<MediaFile> TryCreateFileAsync(string name, bool saveToDisk = false, string userId = null)
        {
            MediaFile media = null;
            try
            {
                media = await CreateFileAsync(name, saveToDisk, userId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return media;
        }

        protected async Task<MediaFile[]> TryCreateAllFilesAsync(string name, bool saveToDisk = false, string userId = null)
        {
            MediaFile[] media = null;
            try
            {
                media = await CreateAllFilesAsync(name, saveToDisk, userId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return media;
        }

        protected string CreateSubDirectory(ref string yearMonth, string userId, string parent = null)
        {
            parent = parent ?? MediaPath;
            var dir = yearMonth = Path.Combine(userId, $"{DateTime.Today.Year}", $"{DateTime.Today.Month.ToString("00")}");

            if (!string.IsNullOrWhiteSpace(parent))
            {
                dir = Path.Combine(parent, dir);
            }

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception)
            {
                dir = string.Empty;
            }

            return dir;
        }

        protected async Task<List<MediaFile>> GetImagesAsync(string userId, string name = null, bool noTracking = false)
        {
            return await GetAllFilesAsync(userId, "image", name, noTracking);
        }

        protected async Task<List<MediaFile>> GetAudioFilesAsync(string userId, string name = null, bool noTracking = false)
        {
            return await GetAllFilesAsync(userId, "audio", name, noTracking);
        }

        protected async Task<List<MediaFile>> GetVideosAsync(string userId, string name = null, bool noTracking = false)
        {
            return await GetAllFilesAsync(userId, "video", name, noTracking);
        }

        protected async Task<List<MediaFile>> GetAllFilesAsync(string userId, string type = null, string name = null, bool noTracking = false)
        {
            IQueryable<MediaFile> query = Database.MediaFiles.Where(f => f.UserId == userId);

            if (noTracking) query = query.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.ContentType.StartsWith(type));
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(f => f.FileName.Contains(name) || f.MediaUrl.Contains(name));
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Method NOT implemented and throws <see cref="NotImplementedException"/>!
        /// When implemented, should create thumbnail images based on the specified image stream.
        /// </summary>
        /// <param name="imageStream">The stream containing the image to resize.</param>
        /// <param name="path">The fully-qualified path of the image to resize.</param>
        /// <param name="contentType">The content type of the stream.</param>
        /// <returns></returns>
        protected Task CreateThumbnailImagesAsync(Stream imageStream, string path, string contentType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts the name and extension from <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The file name to extract information from.</param>
        /// <param name="name">Returns <paramref name="fileName"/> without its extension.</param>
        /// <param name="ext">Returns the extension of <paramref name="fileName"/>.</param>
        public static void ExtractFileNameParts(string fileName, ref string name, ref string ext)
        {
            name = Path.GetFileNameWithoutExtension(fileName);
            ext = Path.GetExtension(fileName);
        }

        /// <summary>
        /// Removes all illegal characters from the given <paramref name="input"/>, optionally keeping white spaces.
        /// </summary>
        /// <param name="input">The input, especiallly file name, to clean.</param>
        /// <param name="keepWhiteSpaces">true to leave white space within the input.</param>
        /// <param name="replacement">The string used to replace unwanted characters with.</param>
        /// <returns></returns>
        public static string SanitizeInput(string input, bool keepWhiteSpaces = false, string replacement = "-")
        {
            const string WHITE = @"\s";
            const string CUSTOM = @"/~!@#%,;&_+=`'\?\<\>\§\^\$\*\(\)\{\}\[\]\|\\";

            var pattern = (keepWhiteSpaces ? "" : WHITE) + CUSTOM + InvalidFileNameRegEx;

            // replace all invalid characters with a hyphen '-'
            var value = Regex.Replace(input.Trim(), $"[{pattern}]+", match => replacement);

            // replace multiple contigous hyphens with a single one
            return Regex.Replace(value, $"[{replacement}]+", m => replacement);
        }

        #endregion

        /// <summary>
        /// Redirects to the specified return (if it's a local) URL; otherwise, redirects to an internal controller.
        /// </summary>
        /// <param name="returnUrl">The URL to redirect to, if local.</param>
        /// <param name="loggedIn">
        /// If true and <paramref name="returnUrl"/> is not local, redirects to the 
        /// <see cref="Areas.Music.Controllers.AlbumsController.Index"/> action.
        /// </param>
        /// <returns></returns>
        protected IActionResult RedirectToLocal(string returnUrl, bool loggedIn = false)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            if (loggedIn)
            {
                return RedirectToAction
                (
                    nameof(Areas.Music.Controllers.AlbumsController.Index),
                    "Albums",
                    new { area = "Music" }
                );
            }
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        /// <summary>
        /// Checks whether a collection with the specified <paramref name="id"/> exists in the database.
        /// </summary>
        /// <param name="id">The identifier of the collection to check.</param>
        /// <returns></returns>
        protected bool CurrentAlbumExists(long id) => Database.MediaAlbums.Any(e => e.Id == id && e.UserId == GetUserId());

        /// <summary>
        /// Returns a collection for the currently authenticated user.
        /// </summary>
        /// <param name="id">The identifier of the collection to find.</param>
        /// <param name="noTracking">
        /// true to disable entity tracking; useful when no changes are intended to be made for the entity.
        /// </param>
        /// <returns></returns>
        protected async Task<MediaAlbum> GetCurrentAlbumAsync(long? id, bool noTracking = false)
            => await GetAlbumAsync(id, await GetUserIdAsync());

        /// <summary>
        /// Returns all collections for the currently authenticated user.
        /// </summary>
        /// <param name="noTracking">
        /// true to disable entity tracking; useful when no changes are intended to be made for the entity.
        /// </param>
        /// <returns></returns>
        protected async Task<List<MediaAlbum>> GetAllCurrentAlbumsAsync(bool noTracking = false)
            => await GetAllAlbumsAsync(await GetUserIdAsync(), noTracking);

        /// <summary>
        /// Returns a media collection, optionally for a given user.
        /// </summary>
        /// <param name="id">The identifier of the collection to find.</param>
        /// <param name="userId">The identifier of the user for whom to return a collection.</param>
        /// <param name="noTracking">
        /// true to disable entity tracking; useful when no changes are intended to be made for the entity.
        /// </param>
        /// <returns></returns>
        protected async Task<MediaAlbum> GetAlbumAsync(long? id, string userId = null, bool noTracking = false)
        {
            if (id == null) return null;

            IQueryable<MediaAlbum> query = Database.MediaAlbums.Include(a => a.Items);

            if (noTracking)
                // no entity tracking
                query = query.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(userId))
                // filter albums by user id
                query = query.Where(a => a.UserId == userId);

            return await query.SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        }

        /// <summary>
        /// Returns all media collections for a given user.
        /// </summary>
        /// <param name="userId">The user identifier for whom to return the collections. If null, all collections are returned.</param>
        /// <param name="noTracking">If true, tracks the entities for changes.</param>
        /// <returns></returns>
        protected async Task<List<MediaAlbum>> GetAllAlbumsAsync(string userId = null, bool noTracking = false)
        {
            IQueryable<MediaAlbum> query = Database.MediaAlbums.Include(a => a.Items);

            if (noTracking)
                query = query.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);

            return await query.ToListAsync();
        }
    }
}
