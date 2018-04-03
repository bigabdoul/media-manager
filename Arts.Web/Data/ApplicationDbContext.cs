using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Arts.Web.Models;
using Arts.Entity.Models;

namespace Arts.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MediaCollection> Albums { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<MediaItem> MediaItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MediaCollection>().ToTable("Album");
            builder.Entity<MediaFile>().ToTable("MediaFile");
            builder.Entity<MediaItem>().ToTable("MediaItem");
        }
    }
}
