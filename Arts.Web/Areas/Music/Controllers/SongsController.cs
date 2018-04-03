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
    public class SongsController : ProjectControllerBase
    {
        public SongsController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
            : base(context, environment)
        {
        }

        // GET: Music/Songs
        public async Task<IActionResult> Index()
        {
            var collection = await GetUserSongsAsync(await GetUserIdAsync());
            return View(collection);
        }

        // GET: Music/Songs/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            var song = await GetSongAsync(id);
            if (song == null)
            {
                return NotFound();
            }

            return View(song);
        }

        // GET: Music/Songs/Create
        public async Task<IActionResult> Create()
        {
            await RetrieveAlbumsAsync();
            return View();
        }

        // POST: Music/Songs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MAX_SIZE_AUDIO)]
        public async Task<IActionResult> Create([Bind("Title,MediaUrl,IsFavorite,MediaAlbumId,MediaName")] MediaAlbumItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = await GetUserIdAsync();
                var media = await TryCreateFileAsync("MediaUrl", true, userId);

                if (media != null)
                {
                    Database.Add(media);

                    if (!string.IsNullOrWhiteSpace(model.MediaName))
                    {
                        media.FileName = SanitizeInput(model.MediaName, true);
                    }

                    // save in order to retrieve the media id from db
                    await Database.SaveChangesAsync();

                    // var album = await GetAlbumAsync(model.MediaAlbumId, userId);

                    var song = new MediaAlbumItem
                    {
                        // MediaAlbum = album,
                        MediaAlbumId = model.MediaAlbumId,
                        IsFavorite = model.IsFavorite,
                        MediaType = MediaType.Audio,
                        Title = model.Title,
                        UserId = userId,
                        MediaFileId = media.Id,
                        MediaUrl = media.GetMediaUrl()
                    };

                    Database.Add(song);
                    await Database.SaveChangesAsync();

                    return RedirectToLocal(Request.Form["ReturnUrl"], true);
                }
                else
                {
                    ModelState.AddModelError("", "Couldn't save the audio file. Please try again.");
                }
            }
            await RetrieveAlbumsAsync();
            return View(model);
        }

        // GET: Music/Songs/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            var song = await GetSongAsync(id);
            if (song == null)
            {
                return NotFound();
            }
            await RetrieveAlbumsAsync(song.MediaAlbumId);
            return View(song);
        }

        // POST: Music/Songs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MAX_SIZE_AUDIO)]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Title,IsFavorite,MediaUrl,MediaAlbumId,MediaName")] MediaAlbumItemViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await GetSongAsync(id, await GetUserIdAsync());

                    if (entity != null)
                    {
                        entity.Title = model.Title;
                        // entity.MediaUrl = model.MediaUrl;
                        entity.IsFavorite = model.IsFavorite;
                        entity.MediaAlbumId = model.MediaAlbumId;

                        Database.Update(entity);
                        await Database.SaveChangesAsync();

                        return RedirectToAction(nameof(Details), new { id });
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SongExists(model.Id))
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
            await RetrieveAlbumsAsync(model.MediaAlbumId);
            return View(model);
        }

        // GET: Music/Songs/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            var song = await GetSongAsync(id);
            if (song == null)
            {
                return NotFound();
            }
            return View(song);
        }

        // POST: Music/Songs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var song = await GetSongAsync(id);
            if (song != null)
            {
                Database.MediaAlbumItems.Remove(song);

                if (MediaFileExists(song.MediaFileId, out var media, await GetUserIdAsync()))
                {
                    Database.Remove(media);
                }

                await Database.SaveChangesAsync();
                var t = TryDeletePhysicalFile(song.MediaUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        protected bool SongExists(long id)
        {
            return GetMediaItems(MediaType.Audio).Any(e => e.Id == id && e.UserId == GetUserId());
        }

        protected async Task<MediaAlbumItem> GetSongAsync(long? id, string userId = null)
        {
            if (id == null) return null;

            IQueryable<MediaAlbumItem> query = GetMediaItems(MediaType.Audio).Include(s => s.MediaAlbum);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(s => s.UserId == userId);

            return await query.SingleOrDefaultAsync(m => m.Id == id);
        }

        protected async Task<List<MediaAlbumItem>> GetUserSongsAsync(string userId = null)
        {
            IQueryable<MediaAlbumItem> query = GetMediaItems(MediaType.Audio).Include(s => s.MediaAlbum);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(s => s.UserId == userId);

            return await query.ToListAsync();
        }

        private async Task RetrieveAlbumsAsync(long? current = null)
        {
            var albums = await GetAllCurrentAlbumsAsync(noTracking: true);
            albums.Insert(0, new MediaAlbum { Title = "[Please select an album]" });
            ViewBag.MediaAlbumIdList = albums;
            ViewData["MediaAlbumId"] = new SelectList(albums, "Id", "Title");

            if (current != null && current.Value > 0L)
            {
                ViewBag.MediaAlbum = albums.SingleOrDefault(a => a.Id == current.Value);
            }
        }
    }
}
