using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighlyDeveloped.Core.ViewModel
{
    public class ForgottenPasswordViewModel
    {
        [DisplayName("EmailAddress")]
        // Required field
        [Required(ErrorMessage = "Please enter a valid email address")]
        // EmailAddress validation
        [EmailAddress(ErrorMessage = "Please enter a valid email adress")]
        public string EmailAddress { get; set; }
    }
}
