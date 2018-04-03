using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arts.Entity.Models;

namespace Arts.Web.ViewModels
{
    public class ChangeAlbumViewModel : MediaAlbumItemViewModel, IMediaAlbum
    {
        [StringLength(250)]
        public string Author { get; set; }

        [StringLength(100)]
        public string Genre { get; set; }

        public override string Display => $"{Title}{(string.IsNullOrWhiteSpace(Author) ? "" : $" - {Author}")}";
    }
}
