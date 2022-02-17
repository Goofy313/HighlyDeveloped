using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighlyDeveloped.Core.ViewModel
{
    // The viewmodel for the registration page. EqualTo() must be installed via the package manager.
    public class RegisterViewModel
    {
        [DisplayName("First Name")]
        [Required(ErrorMessage = "Please enter your first name")]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        [Required(ErrorMessage = "Please enter your Last name")]
        public string LastName { get; set; }
        [DisplayName("Username")]
        [Required(ErrorMessage = "Please enter a username")]
        [MinLength(6)]
        public string Username { get; set; }
        [DisplayName("Email")]
        [Required(ErrorMessage = "Please enter your Email Address")]
        public string EmailAddress { get; set; }
        [UIHint("Password")]
        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter a password")]
        [MinLength(10, ErrorMessage = "Please make your password at least 10 characters long")]
        public string Password { get; set; }
        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "Please re enter your password")]
        [EqualTo("Password", ErrorMessage = "Please ensure your passwords match")]
        public string ConfirmPassword { get; set; }
    }
}
