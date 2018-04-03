using System.ComponentModel.DataAnnotations;

namespace Arts.Entity.Models
{
    public interface IMediaAlbum : IMedia
    {
        string Author { get; set; }
        string Genre { get; set; }
    }
}