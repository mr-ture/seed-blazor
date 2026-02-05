using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Okta.AspNetCore;

namespace MIF.UI.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        public IActionResult SignIn([FromQuery] string returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            return Challenge(OktaDefaults.MvcAuthenticationScheme);
        }

        public IActionResult SignOut([FromQuery] string returnUrl)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            return SignOut(
                new AuthenticationProperties() { RedirectUri = Url.Content("~/") },
                new[]
                {
                    OktaDefaults.MvcAuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                });
        }
    }
}
