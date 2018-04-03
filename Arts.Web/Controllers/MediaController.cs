using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Arts.Entity.Data;
using Arts.Entity.Models;

namespace Arts.Web.Controllers
{
    [Authorize]
    public class MediaController : ProjectControllerBase
    {
        public MediaController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
            : base(context, environment)
        {
        }

        public async Task<IActionResult> Index(string id, string name)
        {
            var mediaFiles = await GetAllFilesAsync(await GetUserIdAsync(), id, name, noTracking: true);
            return View(mediaFiles);
        }

        // GET: Media/Details/5
        public async Task<IActionResult> Details(long? id, int? refs)
        {
            if (!MediaFileExists(id, out var media, await GetUserIdAsync()))
            {
                return NotFound();
            }
            ViewBag.ReferenceCount = refs;
            return View(media);
        }

        // GET: Media/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: Media/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (!MediaFileExists(id, out var media, await GetUserIdAsync()))
            {
                return NotFound();
            }

            return View(media);
        }

        // POST: Media/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Id,FileName,MediaUrl")] MediaFile model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (MediaFileExists(id, out var media, await GetUserIdAsync()))
                    {
                        media.FileName = SanitizeInput(model.FileName, true);
                        if (model.HasMedia)
                        {
                            // change the media url...
                            var url = model.MediaUrl;
                            media.MediaUrl = url;

                            // ... and update all album items that reference the media file being updated.
                            await Database.MediaAlbumItems
                                .Where(m => m.MediaFileId == media.Id)
                                .ForEachAsync(item => item.MediaUrl = url);
                        }
                        Database.Update(media);
                        await Database.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MediaFileExists(model.Id))
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
            return View(model);
        }

        // GET: Media/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (!MediaFileExists(id, out var media, await GetUserIdAsync()))
            {
                return NotFound();
            }

            return View(media);
        }

        // POST: Media/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var count = await DeleteMediaFile(id);
            if (count <= 1)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction(nameof(Details), new { id, refs = count });
            }
        }

        public async Task<IActionResult> Play(long id)
        {
            if (MediaFileExists(id, out var media, await GetUserIdAsync()))
            {
                if (media.IsAudio || media.IsVideo)
                {
                    // find the first album item that references the media file to play
                    var albumItem = await Database.MediaAlbumItems
                        .Include(item => item.MediaAlbum) // include album info
                        .Where(item => item.MediaFileId == id)
                        .FirstOrDefaultAsync();

                    // get the album which the item belongs to
                    ViewBag.MediaAlbum = albumItem?.MediaAlbum;
                    ViewBag.IsMediaPlayer = true;
                    return View(media);
                }
                else
                {
                    return File(media.MediaUrl, media.ContentType);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MediaFileExists(long? id)
        {
            return MediaFileExists(id, out var m, GetUserId());
        }
    }
}
