using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HighlyDeveloped.Core.ViewModel
{
    public class ContactFormViewModel
    {
        [Required]
        [MaxLength(80, ErrorMessage = "Limit to 80 characters")]
        public string Name { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Invalid E-mail")]
        public string EmailAddress { get; set; }
        [Required]
        [MaxLength(500, ErrorMessage = "Limit to 500 characters")]
        public string Comment { get; set; }
        [MaxLength(255, ErrorMessage = "Limit to 255 characters")]
        public string Subject { get; set; }
    }
}
