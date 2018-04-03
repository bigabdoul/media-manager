using System;
using System.ComponentModel.DataAnnotations;
using Arts.Entity.Models;

namespace Arts.Web.ViewModels
{
    public class MediaAlbumItemViewModel : IMediaAlbumItem
    {
        public long Id { get; set; }

        [StringLength(250), Display(Name = "Media Path")]
        public string MediaUrl { get; set; }

        public long MediaFileId { get; set; }

        [Required, StringLength(250)]
        public string Title { get; set; }

        [Display(Name = "Favorite?")]
        public bool IsFavorite { get; set; }

        [Display(Name = "Album")]
        public long MediaAlbumId { get; set; }

        [Display(Name = "Media Type")]
        public MediaType MediaType { get; set; }

        [Display(Name = "Created")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Modified")]
        public DateTime DateModified { get; set; }

        [Display(Name = "File Name (Optional)"), StringLength(100)]
        public string MediaName { get; set; }
        
        public bool HasMedia => !string.IsNullOrWhiteSpace(MediaUrl);

        public virtual string Display => Title;

        public override string ToString()
        {
            return Display;
        }
    }
}
