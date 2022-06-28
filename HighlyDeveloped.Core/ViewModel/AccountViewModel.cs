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
    // This is the view model for the my account page
    public class AccountViewModel
    {
        [DisplayName("Fullname")]
        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; }
        [DisplayName("Email")]
        [Required(ErrorMessage = "Please enter your email")]
        [Email(ErrorMessage = "Please enter a valid email")]
        public string Email { get; set; }
        public string Username { get; set; }
        [UIHint("Password")]
        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter a password")]
        [MinLength(10, ErrorMessage = "Please make your password at least 10 characters long")]
        public string Password { get; set; }
        [UIHint("Confirm Password")]
        [DisplayName("Confirm Password")]
        [EqualTo("Password", ErrorMessage = "Please ensure your passwords match")]
        public string ConfirmPassword { get; set; }
    }
}
