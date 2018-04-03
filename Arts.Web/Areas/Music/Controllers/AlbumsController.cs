using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arts.Entity.Data;
using Arts.Entity.Models;
using Arts.Web.Controllers;
using Arts.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Arts.Web.Areas.Music.Controllers
{
    [Authorize]
    [Area("Music")]
    public class AlbumsController : ProjectControllerBase
    {
        public AlbumsController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
            : base(context, environment)
        {
        }

        // GET: Music/Albums
        public async Task<IActionResult> Index(string q)
        {
            return View(await GetAllCurrentAlbumsAsync(true));
        }

        // GET: Music/Albums/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var album = await GetCurrentAlbumAsync(id, true);
            if (album == null)
            {
                return NotFound();
            }
            return View(album);
        }

        // GET: Music/Albums/Create
        public async Task<IActionResult> Create()
        {
            await RetrieveExistingImages();
            return View();
        }

        // POST: Music/Albums/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MAX_SIZE_IMAGE)]
        public async Task<IActionResult> Create([Bind("Id,Author,Title,Genre,MediaUrl,MediaName,MediaFileId")] ChangeAlbumViewModel model)
        {
            if (ModelState.IsValid)
            {
                var album = new MediaAlbum
                {
                    Author = model.Author,
                    Title = model.Title,
                    Genre = model.Genre,
                    UserId = await GetUserIdAsync(),
                    MediaFileId = model.MediaFileId,
                };

                await CreateOrUpdateCoverAsync(album, model.MediaName);
                Database.Add(album);
                await Database.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = album.Id });
            }
            await RetrieveExistingImages(model.MediaFileId);
            return View(model);
        }

        // GET: Music/Albums/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var album = await GetCurrentAlbumAsync(id, true);
            if (album == null)
            {
                return NotFound();
            }
            await RetrieveExistingImages(album.MediaFileId);
            return View(album);
        }

        // POST: Music/Albums/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MAX_SIZE_IMAGE)]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Author,Title,Genre,MediaUrl,MediaName,MediaFileId")] ChangeAlbumViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var album = await GetAlbumAsync(id, await GetUserIdAsync());

                    if (album != null)
                    {
                        album.Author = model.Author;
                        album.Title = model.Title;
                        album.Genre = model.Genre;
                        album.MediaUrl = model.MediaUrl;
                        album.DateModified = DateTime.Now;
                        
                        await CreateOrUpdateCoverAsync(album, model.MediaName);
                        Database.Update(album);
                        await Database.SaveChangesAsync();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CurrentAlbumExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            await RetrieveExistingImages(model.MediaFileId);
            return View(model);
        }

        // GET: Music/Albums/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var album = await GetCurrentAlbumAsync(id, true);

            if (album == null)
            {
                return NotFound();
            }

            return View(album);
        }

        // POST: Music/Albums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var album = await GetCurrentAlbumAsync(id);
            if (album != null)
            {
                var songs = album.Items.ToArray();

                Database.MediaAlbums.Remove(album);
                await Database.SaveChangesAsync();

                foreach (var song in songs)
                {
                    var t = TryDeletePhysicalFile(song.MediaUrl);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Favorite(int id)
        {
            bool success = false, favorite = false;
            var album = await GetCurrentAlbumAsync(id);
            if (album != null)
            {
                favorite = !album.IsFavorite;
                album.IsFavorite = favorite;
                await Database.SaveChangesAsync();
                success = true;
            }
            return Json(new { success, favorite });
        }
        
        #region helper methods

        /// <summary>
        /// Creates or updates a cover photo for the specified album.
        /// </summary>
        /// <param name="album">The album to create or update the cover photo for.</param>
        /// <param name="mediaName">Optional: the display name of the media file.</param>
        /// <returns></returns>
        private async Task CreateOrUpdateCoverAsync(MediaAlbum album, string mediaName = null)
        {
            const string FILE_NAME = "MediaUrl";

            var saveToDisk = true;
            var userId = await GetUserIdAsync();
            album.UserId = userId;

            if (!HasUploadedFile(FILE_NAME))
            {
                // if no file was uploaded, check for an existing media
                var fileId = Request.Form["MediaFileId"];

                if (long.TryParse(fileId, out var mfid) && MediaFileExists(mfid, out var file, userId))
                {
                    // a stored media file exists, use it for the cover photo
                    album.MediaFileId = mfid;
                    album.MediaUrl = file.GetMediaUrl();
                }
                else
                {
                    album.MediaFileId = 0;
                    album.MediaUrl = null;
                }
            }
            else if ((await TryCreateFileAsync(FILE_NAME, saveToDisk, userId)) is MediaFile mf && mf != null)
            {
                // a media file has been created on disk, check if it's not already stored in the database
                if (!MediaFileExists(mf.HashCode, out MediaFile media, userId))
                {
                    if (!string.IsNullOrWhiteSpace(mediaName))
                    {
                        mf.FileName = SanitizeInput(mediaName, true);
                    }
                    // add to the database
                    Database.MediaFiles.Add(mf);
                    await Database.SaveChangesAsync();
                }
                else
                {
                    // the exact same media file already exists in the database
                    // grab a reference and use it for the current album
                    mf = media;
                }
                album.MediaFileId = mf.Id;
                album.MediaUrl = mf.GetMediaUrl();
            }
            else
            {
                // try to set the original cover photo
                var entity = await GetAlbumAsync(album.Id, userId, noTracking: true);

                if (entity != null)
                {
                    album.MediaUrl = entity.MediaUrl;
                    album.MediaFileId = entity.MediaFileId;
                }
            }
        }

        private async Task<List<MediaFile>> RetrieveExistingImages(long selectedValue = 0)
        {
            var list = await GetImagesAsync(await GetUserIdAsync(), noTracking: true);
            list.Insert(0, new MediaFile { FileName = "[Please select a picture]" });
            ViewBag.MediaFileIdList = list;
            ViewBag.MediaFileId = new SelectList(list, "Id", "FileName", selectedValue);
            return list;
        }
        #endregion
    }
}
