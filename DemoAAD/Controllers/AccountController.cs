using System.Web;
using System.Web.Mvc;
using DemoAAD.Utils;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;

namespace DemoAAD.Controllers
{
    public class AccountController : Controller
    {


		public void SignIn()
		{
			// Send an OpenID Connect sign-in request.
			if (!Request.IsAuthenticated)
			{
				HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
			}
		}
		public void SignOut()
		{
			// Remove all cache entries for this user and send an OpenID Connect sign-out request.
			TokenCacheUtils.RemoveAllFromCache();
			HttpContext.GetOwinContext().Authentication.SignOut(
				OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
		}
    }
}
