using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using System.Net.Mail;
using HighlyDeveloped.Core.Interfaces;

namespace HighlyDeveloped.Core.Controllers
{
    // This is for operations regarding the contact form
    public class ContactController : SurfaceController
    {
        private IEmailService _emailService;

        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }
        public ActionResult RenderContactForm()
        {
            var vm = new ContactFormViewModel();
            return PartialView("~/Views/Partials/Contact Form.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleContactForm(ContactFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Check form.");
                return CurrentUmbracoPage();
            }

            try
            {
                // Create new contact form within Umbraco 
                // Get a handle to "Contact Forms"

                var contactForms = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("contactForms").FirstOrDefault();

                if (contactForms != null)

                {
                    var newContact = Services.ContentService.Create("Contact", contactForms.Id, "contactForm");
                    newContact.SetValue("contactName", vm.Name);
                    newContact.SetValue("contactEmail", vm.EmailAddress);
                    newContact.SetValue("contactSubject", vm.Subject);
                    newContact.SetValue("contactComment", vm.Comment);
                    Services.ContentService.SaveAndPublish(newContact);
                }

                // Send out an email to site admin
                //SendContactFormReceivedEamil(vm);

                _emailService.SendContactNotificationToAdmin(vm);

                // Return confirmation message

                TempData["status"] = "OK";

                return RedirectToCurrentUmbracoPage();
            }
            catch (Exception exc)
            {
                Logger.Error<ContactController>("Error in contact form submission", exc.Message);
                ModelState.AddModelError("Error", "Problem regarding your details");
            }

            return CurrentUmbracoPage();
        }

        // This will sent out a submitted email to site admins

        private void SendContactFormReceivedEamil(ContactFormViewModel vm)
        {
            // Get site settings

            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            // Read email FROM and TO addresses

            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");
            var toAddress = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("There needs to be a from address in the site settings");
            }
            if (string.IsNullOrEmpty(toAddress))
            {
                throw new Exception("There needs to be a to address in the site settings");
            }

            // Construct the email

            var emailSubject = "There has been a contact form submitted";
            var emailBody = $"A new contact form has been received from {vm.Name}. Their comments were {vm.Comment}";
            var smtpMessage = new MailMessage();
            smtpMessage.Subject = emailSubject;
            smtpMessage.Body = emailBody;
            smtpMessage.From = new MailAddress(fromAddress);

            var toList = toAddress.Split(',');
            foreach (var item in toList)
            {
                if (!string.IsNullOrEmpty(item))
                    smtpMessage.To.Add(item);
            }

            // Send via email service

            using(var smtp = new SmtpClient())
            {
                smtp.Send(smtpMessage);
            }
        }
    }
}
