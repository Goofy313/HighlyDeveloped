using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    public class AccountController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/MyAccount/";
        public ActionResult RenderMyAccount()
        {
            var vm = new AccountViewModel();

            // Are we logged in?
            if (!Umbraco.MemberIsLoggedOn())
            {
                ModelState.AddModelError("Error", "You aren't currently looged in");
                return CurrentUmbracoPage();
            }

            // Get members details
            // Membership from (Membership.GetUser().UserName is ASP.NET membership provider not Umbraco
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if (member == null)
            {
                ModelState.AddModelError("Error", "You're not in the system.");
                return CurrentUmbracoPage();
            }
            // Populate the vm accordingly
            vm.Name = member.Name;
            vm.Email = member.Email;
            vm.Username = member.Username;

            return PartialView(PARTIAL_VIEW_FOLDER + "MyAccount.cshtml", vm);
        }

        [HttpPost]
        // ValidateAntiForgeryToken protects against cross site attacks
        [ValidateAntiForgeryToken]
        public ActionResult HandleUpdateDetails(AccountViewModel vm)
        {
            // Is the model valid
            // This is not working
            //if (!ModelState.IsValid)
            //{
            //    ModelState.AddModelError("Error", "There was a problem");
            //    return CurrentUmbracoPage();
            //}

            // Is there a member?
            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "You're not logged on.");
                return CurrentUmbracoPage();
            }

            // Get member by username
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if (member == null)
            {
                ModelState.AddModelError("Error", "You're not logged on.");
                return CurrentUmbracoPage();
            }

            // Update the members details
            member.Name = vm.Name;
            member.Email = vm.Email;
            Services.MemberService.Save(member);

            // Thanks
            TempData["status"] = "Updated Details";
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandlePasswordChange(AccountViewModel vm)
        {
            // Model valid 
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "There is a problem with the form");
                return CurrentUmbracoPage();
            }

            // Member valid

            if (!Umbraco.MemberIsLoggedOn() || Membership.GetUser() == null)
            {
                ModelState.AddModelError("Error", "You're not logged in");
                return CurrentUmbracoPage();
            }

            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if (member == null)
            {
                ModelState.AddModelError("Error", "You're not logged in");
                return CurrentUmbracoPage();
            }

            // Update the password
            try
            {
                Services.MemberService.SavePassword(member, vm.Password);
            }
            catch (MembershipPasswordException exc)
            {
                ModelState.AddModelError("Error", "Error with password" + exc.Message);
                return CurrentUmbracoPage();
            }

            // Thanks - Updated Password
            TempData["status"] = "Udated Password";
            return RedirectToCurrentUmbracoPage();
        }
    }
}
