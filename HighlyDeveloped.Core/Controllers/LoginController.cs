using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;

namespace HighlyDeveloped.Core.Controllers
{
    // Bespoke login process
    public class LoginController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/Login/";

        // Inject _email.Service for SendResetPassword
        // Reference to IEmailService #local reference
        private IEmailService _emailService;
        // IEmailService is injected into the LoginController ctor
        public LoginController(IEmailService emailService)
        {
            // Set #local reference to injected
            _emailService = emailService;
        }

        #region Login
        // Here is our render
        public ActionResult RenderLogin()
        {
            var vm = new LoginViewModel();
            vm.RedirectUrl = HttpContext.Request.Url.AbsolutePath;
            return PartialView(PARTIAL_VIEW_FOLDER + "Login.cshtml", vm);
        }

        // Our handle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleLogin(LoginViewModel vm)
        {
            // Check if model is ok
            if(!ModelState.IsValid)
            {
                // Error message automatically rendered
                return CurrentUmbracoPage();
            }

            // Check if member exists with that username
            var member = Services.MemberService.GetByUsername(vm.Username);
            if (member == null)
            {
                ModelState.AddModelError("Login", "Cannot find that username in the system");
                return CurrentUmbracoPage();
            }

            // Check if memeber tried to log in to memeber times and is locked out
            // Added maxInvalidPasswordAttempts="5" to Web.config 
            if (member.IsLockedOut)
            {
                ModelState.AddModelError("Login", "Your account is locked, please use forgotten password to reset");
                return CurrentUmbracoPage();
            }

            // Check if they have validated their email address
            // emailVerified was added to be true if the user clicked on verfiy link
            var emailVerified = member.GetValue<bool>("emailVerified");
            if (!emailVerified)
            {
                ModelState.AddModelError("Login", "Please verify your email before logging in.");
                return CurrentUmbracoPage();
            }

            // Check if credentials are check
            // Log them in
            // Because we're in the surface controller we have access to Members
            if (!Members.Login(vm.Username, vm.Password))
            {
                //If this fails 5 times the account will be locked because we added maxInvalidPasswordAttempts="5" to Web.config
                ModelState.AddModelError("Login", "The username/password you provided is incorrect");
                return CurrentUmbracoPage();
            }

            // Passed validation and should be logged in
            // Redirect them back on their way of where they wanted to go originally before they were asked to log in orginally
            if (!string.IsNullOrEmpty(vm.RedirectUrl))
            {
                return Redirect(vm.RedirectUrl);
            }
            // Otherwise just redirect to the current page
            return RedirectToCurrentUmbracoPage();
        }
        #endregion

        #region Forgotten Password
        public ActionResult RenderForgottenPassword()
        {
            // instantiate new viewModel
            var vm = new ForgottenPasswordViewModel();
            
                return PartialView(PARTIAL_VIEW_FOLDER + "ForgottenPassword.cshtml", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgottenPassword(ForgottenPasswordViewModel vm)
        {
            // Is the model ok?
            if (!ModelState.IsValid)
            {
                // will return an error message based on the validation set in the view model itself
                return CurrentUmbracoPage();
            }

            // Do we have a member with this email address
            // If not, error
            //var member = Members.GetByEmail(vm.EmailAddress); returns IPublished Content as read only
            //var member = Services.MemberService.GetByEmail(vm.EmailAddress); returns IMember which allows to do updates
            var member = Services.MemberService.GetByEmail(vm.EmailAddress);
            if (member == null)
            {
                ModelState.AddModelError("Error", "Sorry we can't find that email address in the system");
                return CurrentUmbracoPage();
            }

            // Create the reset token
            var resetToken = Guid.NewGuid().ToString();

            // Set the reset expiray date (now + 12 hours)
            var expiryDate = DateTime.Now.AddHours(12);

            // Save to member
            member.SetValue("resetExpiryDate", expiryDate);
            member.SetValue("resetLinkToken", resetToken);
            Services.MemberService.Save(member);

            // Fire the email - reset password
            // Called _emailService.SendResetPasswordNotification from injected injected IEmailService
            _emailService.SendResetPasswordNotification(vm.EmailAddress, resetToken);

            // Log details
            // using Umbraco.Core.Logging
            Logger.Info<LoginController>($"Sent a password reset to {vm.EmailAddress}");

            // Thanks
            TempData["status"] = "OK";

            // When redirecting, this prevents double form submission when refreshing the page
            return RedirectToCurrentUmbracoPage();
        }
        #endregion

        #region Reset Password
        public ActionResult RenderResetPassword()
        {
            // Instatiate ResetPasswordViewModel()
            var vm = new ResetPasswordViewModel();
            // Add vm to partial function
            return PartialView(PARTIAL_VIEW_FOLDER + "ResetPassword.cshtml", vm);
        }

        // Comes from a HTTP post
        [HttpPost]
        // Prevent cross-site request forgery attacks like a malicious command from the browser of a trusted user
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(ResetPasswordViewModel vm)
        {
            // Get reset token
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            // Ensure we have the token
            var token = Request.QueryString["token"];
            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn<LoginController>("Request Password - no token found");
                ModelState.AddModelError("Error", "Invalid Token");
                return CurrentUmbracoPage();
            }

            // Get the member for the token
            // MemberService gives IMember used for updating
            // GetMembersByPropertyValue is useful as gives any member whose resetLinkToken equals the token coming in.
            // SingleOrDefault means to only expect one and if there isn't one it can be null
            var member = Services.MemberService.GetMembersByPropertyValue("resetLinkToken", token).SingleOrDefault();
            if (member == null)
            {
                ModelState.AddModelError("Error", "That link is no longer valid");
                return CurrentUmbracoPage();
            }

            // Check the time window hasn't expired
            // GetValue of DateTime and call it resetExpiryDate
            var membersTokenExpiryDate = member.GetValue<DateTime>("resetExpiryDate");
            // Get current time
            var currentTime = DateTime.Now;
            if (currentTime.CompareTo(membersTokenExpiryDate) >= 0)
            {
                ModelState.AddModelError("Error", "That link has expired, please use the forgotten password link to regenerate a new one");
                return CurrentUmbracoPage();
            }

            // If ok, update the password for member
            // this will fail unless you update allowManuallyChangingPassword="false" in web.config
            Services.MemberService.SavePassword(member, vm.Password);
            // reset link token and expiry date to blank
            member.SetValue("resetLinkToken", string.Empty);
            member.SetValue("resetExpiryDate", null);
            // maybe the user is locked out of their account so we unlock it
            member.IsLockedOut = false;
            Services.MemberService.Save(member);

            // Send out email
            // Call email and SendPasswordChangedNotification to the member 
            _emailService.SendPasswordChangedNotification(member.Email);

            // Thanks
            TempData["status"] = "OK";
            // Logging purposes
            Logger.Info<LoginController>($"User {member.Username} has changed their password");

            return RedirectToCurrentUmbracoPage();
        }
        #endregion
    }
}
