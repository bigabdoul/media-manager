namespace Arts.Entity.Models
{
    public class Media : MediaEntity, IMedia
    {
        public bool IsFavorite { get; set; }
        public string MediaUrl { get; set; }
        public long MediaFileId { get; set; }
        public string Title { get; set; }
        public bool HasMedia => !string.IsNullOrWhiteSpace(MediaUrl);

        public override string ToString()
        {
            return Title;
        }
    }
}
