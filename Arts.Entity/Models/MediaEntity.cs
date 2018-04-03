using System;

namespace Arts.Entity.Models
{
    public class MediaEntity
    {
        public MediaEntity()
        {
            DateCreated = DateModified = DateTime.UtcNow;
        }

        public long Id { get; set; }

        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }
    }
}
