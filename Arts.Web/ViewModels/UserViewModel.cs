using System;
using System.ComponentModel.DataAnnotations;

namespace Arts.Web.ViewModels
{
    public class UserViewModel
    {
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [StringLength(1)]
        public string Gender { get; set; }

        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string City { get; set; }
    }
}
