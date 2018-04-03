using System;
using Microsoft.AspNetCore.Identity;

namespace Arts.Entity.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            DateCreated = DateModified = DateTime.UtcNow;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public bool IsStaff { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
