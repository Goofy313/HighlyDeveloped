using DataAnnotationsExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighlyDeveloped.Core.ViewModel
{
    public class ResetPasswordViewModel
    {
        [UIHint("Password")]
        [Required(ErrorMessage = "Please enter your new password")]
        // Use RegularExpression to enforse a password policy
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{10,}$", ErrorMessage = "Please enter at least 10 characters, have atleast one special character, number, uppercase")]
        public string Password { get; set; }
        [UIHint("Password")]
        [Required(ErrorMessage = "Please enter your new password")]
        [EqualTo("Password", ErrorMessage = "Please ensure your passwords match")]
        public string ConfrimPassword { get; set; }
    }
}
