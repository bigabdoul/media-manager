using Arts.Entity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Arts.Entity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<MediaAlbum> MediaAlbums { get; set; }
        public DbSet<MediaAlbumItem> MediaAlbumItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MediaFile>().ToTable("MediaFile");
            builder.Entity<MediaAlbum>().ToTable("MediaAlbum");
            builder.Entity<MediaAlbumItem>().ToTable("MediaAlbumItem");
        }
    }
}
