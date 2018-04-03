using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Arts.Entity.Models
{
    public class MediaAlbum : Media, IMediaAlbum
    {
        public MediaAlbum()
        {
            Items = new HashSet<MediaAlbumItem>();
        }

        public string Author { get; set; }
        public string Genre { get; set; }

        public ICollection<MediaAlbumItem> Items { get; set; }

        public override string ToString()
        {
            return $"{Title}{(string.IsNullOrWhiteSpace(Author) ? "" : $" - {Author}")}";
        }
    }
}
