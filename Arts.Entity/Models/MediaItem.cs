namespace Arts.Entity.Models
{
    public class MediaAlbumItem : Media, IMediaAlbumItem
    {
        public MediaAlbum MediaAlbum { get; set; }
        public long MediaAlbumId { get; set; }
        public MediaType MediaType { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
