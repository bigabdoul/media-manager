namespace Arts.Entity.Models
{
    public interface IMediaAlbumItem : IMediaItem
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "Album")]
        long MediaAlbumId { get; set; }
    }
}