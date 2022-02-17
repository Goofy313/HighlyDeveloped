using Umbraco.Core.Logging;
using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace HighlyDeveloped.Core.Services
{
    public class EmailService : IEmailService
    {
        // add ctor injection UmbracoHelper
        private UmbracoHelper _umbraco;
        private IContentService _contentService;
        private ILogger _logger;

        public EmailService(UmbracoHelper umbraco, IContentService contentService, ILogger logger)
        {
            _umbraco = umbraco;
            _contentService = contentService;
            _logger = logger;
        }

        // sending of the email to an admin

        public void SendContactNotificationToAdmin(ContactFormViewModel vm)
        {

            // Get email template from Umbraco for "this" notification

            var emailTemplate = GetEmailTemplate("New Contact Form Notification");

            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            // get the template data

            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            // Mail merge
            // ##name##
            htmlContent = htmlContent.Replace("##name##", vm.Name);
            textContent = htmlContent.Replace("##name##", vm.Name);
            // ##email##
            htmlContent = htmlContent.Replace("##email##", vm.EmailAddress);
            textContent = htmlContent.Replace("##email##", vm.EmailAddress);
            // ##comment##
            htmlContent = htmlContent.Replace("##comment##", vm.Comment);
            textContent = htmlContent.Replace("##comment##", vm.Comment);

            // Send the email

            // Read email out 

            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

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

            // Debug mode

            var debugMode = siteSettings.Value<bool>("testMode");
            var testEmailAccounts = siteSettings.Value<string>("emailTestAccount");

            var recipients = toAddress;

            if (debugMode)
            {
                // Change the To - testEmailAccount
                recipients = testEmailAccounts;
                // Alter subject line - to show in test mode
                subject += "(TEST MODE) - " + toAddress;
            }

            // Log the email in Umbraco
            // Email

            var emails = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("emails").FirstOrDefault();
            var newEmail = _contentService.Create(toAddress, emails.Id, "Email");
            newEmail.SetValue("emailSuject", subject);
            newEmail.SetValue("emailToAddress", recipients);
            newEmail.SetValue("emailHtmlContent", htmlContent);
            newEmail.SetValue("emailTextContent", textContent);
            newEmail.SetValue("emailSent", false);
            _contentService.SaveAndPublish(newEmail);

            // Send the email via SMTP or such

            var mimeType = new System.Net.Mime.ContentType("text/html");
            var alternateView = AlternateView.CreateAlternateViewFromString(htmlContent, mimeType);

            var smtpMessage = new MailMessage();
            smtpMessage.AlternateViews.Add(alternateView);

            //To - one or multipal emails

            foreach (var recipient in recipients.Split(','))
            {
                if (!string.IsNullOrEmpty(recipient))
                {
                    smtpMessage.To.Add(recipient);
                }
            }

            //From
            smtpMessage.From = new MailAddress(fromAddress);
            //Subject
            smtpMessage.Subject = subject;
            //Body
            smtpMessage.Body = textContent;

            //Sending
            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Send(smtpMessage);
                    newEmail.SetValue("emailSent", true);
                    newEmail.SetValue("emailSentDate", DateTime.Now);
                    _contentService.SaveAndPublish(newEmail);
                }
                catch (Exception exc)
                {
                    // Log error
                    _logger.Error<EmailService>("Problem sending the email", exc);
                    throw exc;
                }
            }
        }

        // Returns the email template as IPublishedContent where the title matches the template name
        private IPublishedContent GetEmailTemplate(string templateName)
        {
            var template = _umbraco.ContentAtRoot()
                            .DescendantsOrSelfOfType("emailTemplate")
                            .Where(w => w.Name == templateName)
                            .FirstOrDefault();

            return template;
        }
    }
}
