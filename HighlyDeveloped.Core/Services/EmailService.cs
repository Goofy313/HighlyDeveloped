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
using System.Web;

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
            MailMerge("name", vm.Name, ref htmlContent, ref textContent);
            // ##email##
            MailMerge("email", vm.EmailAddress, ref htmlContent, ref textContent);
            // ##comment##
            MailMerge("comment", vm.Comment, ref htmlContent, ref textContent);

            // Send the email

            // Read email out 

            //Get site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            // Read email TO addresses
            var toAddress = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(toAddress))
            {
                throw new Exception("There needs to be a to address in the site settings");
            }

            SendMail(toAddress, subject, htmlContent, textContent);
        }

        // A generic send mail that logs the email in umbtraco and send via smtp
        private void SendMail(string toAddress, string subject, string htmlContent, string textContent)
        {
            // Get site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();

            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            // Read email FROM address
            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("There needs to be a from address in the site settings");
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

        // Send email verification link to member
        public void SendVerifyEmailAddressNotification(string membersEmail, string verificationToken)
        {
            // Get the template - create a new template in umbraco
            var emailTemplate = GetEmailTemplate("Verify Email");

            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            // Fields from the template
            // get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            // Mail merge
            // Mail merge the necessary fields
            // Build the URL to be th eabsolute URL to the verify page
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"/verify?token={verificationToken}";

            MailMerge("verify-url", url, ref htmlContent, ref textContent);

            // Log the Email
            // Send the Email
            SendMail(membersEmail, subject, htmlContent, textContent);
        }


        private void MailMerge(string token, string value, ref string htmlContent, ref string textContent)
        {
            htmlContent = htmlContent.Replace($"##{token}##", value);
            textContent = htmlContent.Replace($"##{token}##", value);
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

        // Send the reset password link to the user
        public void SendResetPasswordNotification(string membersEmail, string resetToken)
        {
            // Get the template
            // The name of the template Reset Password is set in Umbraco Content - Email templates - Reset Password
            var emailTemplate = GetEmailTemplate("Reset Password");

            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            // Fields from the template
            // get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            // Mail merge
            // This get the URL of the website and removes any reference to any page
            var url = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            // Page is going to be called reset-password and token needs to be resetToken as passed in by SendResetPasswordNotification in IEmailService.cs
            url += $"/reset-password?token={resetToken}";

            // Hyperlink was called ##reset-url## so needs to be the same here
            MailMerge("reset-url", url, ref htmlContent, ref textContent);

            // Send
            SendMail(membersEmail, subject, htmlContent, textContent);
        }

        // Send a note to the user telling them they have changed their password
        public void SendPasswordChangedNotification(string membersEmail)
        {
            // Get template
            var emailTemplate = GetEmailTemplate("Password Changed");

            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            // Get data from template
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            // No Mail merge

            // Send
            // Send
            SendMail(membersEmail, subject, htmlContent, textContent);
        }
    }
}
