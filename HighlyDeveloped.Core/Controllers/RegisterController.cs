using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    // Handle member registration
    public class RegisterController : SurfaceController
    {
        private const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/";
        private IEmailService _emailService;

        public RegisterController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // Render the registration form
        public ActionResult RenderRegister()
        {
            var vm = new RegisterViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "Register.cshtml", vm);
        }

        #region Register Form
        //Render the registration post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleRegister(RegisterViewModel vm)
        {
            // If form isn't valid
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            // Check if there is already a member
            var existingMember = Services.MemberService.GetByEmail(vm.EmailAddress);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There's already a user with that email address.");
                return CurrentUmbracoPage();
            }

            // Check if username is in use
            existingMember = Services.MemberService.GetByUsername(vm.Username);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There's already a user with that username.");
                return CurrentUmbracoPage();
            }

            // Create a member in Umbraco with the details
            var newMember = Services.MemberService.CreateMember(vm.Username, vm.EmailAddress, $"{vm.FirstName} {vm.LastName}", "Member");
            newMember.PasswordQuestion = "";
            newMember.RawPasswordAnswerValue = "";
            // Need to save the member before you can set the password
            Services.MemberService.Save(newMember);
            Services.MemberService.SavePassword(newMember, vm.Password);
            // Assign a role i.e Normal User
            Services.MemberService.AssignRole(newMember.Id, "Normal User");

            // Create email verification token
            // Token creation
            var token = Guid.NewGuid().ToString();
            newMember.SetValue("emailVerifyToken", token);
            Services.MemberService.Save(newMember);

            //Send email verification
            _emailService.SendVerifyEmailAddressNotification(newMember.Email, token);

            // Thank the user
            // Return confirmation message
            TempData["status"] = "OK";

            return RedirectToCurrentUmbracoPage();
        }
        #endregion

        #region Verification
        //Render verification
        public ActionResult RenderEmailVerification(string token)
        {
            // Get token (querystring)
            // Look for member matching token
            var member = Services.MemberService.GetMembersByPropertyValue("emailVerifyToken", token).SingleOrDefault();

            if (member != null)
            {
                // If found set to verified
                var alreadyVerified = member.GetValue<bool>("emailVerified");
                if (alreadyVerified)
                {
                    ModelState.AddModelError("Verified", "You're already verified your email address");
                }
                member.SetValue("emailVerified", true);
                member.SetValue("emailVerifiedDate", DateTime.Now);
                Services.MemberService.Save(member);

                // Thank the user
                TempData["status"] = "OK";
                return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
            }

            // Otherwise a problem
            ModelState.AddModelError("Error", "Apologies, there has been some problem!");
            return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
        }
        #endregion
    }
}
