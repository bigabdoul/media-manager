using System;

namespace Arts.Entity.Models
{
    public interface IMediaItem : IMedia
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "Media Type")]
        MediaType MediaType { get; set; }
    }
}