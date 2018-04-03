using System;
using System.ComponentModel.DataAnnotations;

namespace Arts.Entity.Models
{
    public interface IMedia
    {
        long Id { get; set; }

        string Title { get; set; }

        [Display(Name = "Is Favorite?")]
        bool IsFavorite { get; set; }

        [Display(Name = "Media Path")]
        string MediaUrl { get; set; }

        long MediaFileId { get; set; }

        [Display(Name = "Created")]
        DateTime DateCreated { get; set; }

        [Display(Name = "Modifiied")]
        DateTime DateModified { get; set; }

        bool HasMedia { get; }
    }
}