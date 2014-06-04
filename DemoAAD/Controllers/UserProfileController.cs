using System;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using DemoAAD.Models;
using System.Security.Claims;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Globalization;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using DemoAAD.Utils;

namespace DemoAAD.Controllers
{
	[Authorize]
	public class UserProfileController : Controller
	{
		private readonly string _graphResourceId = ConfigurationManager.AppSettings["ida:GraphUrl"];
		private readonly string _graphUserUrl = "https://graph.windows.net/{0}/me?api-version=" + ConfigurationManager.AppSettings["ida:GraphApiVersion"];
		private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

		//
		// GET: /UserProfile/
		public async Task<ActionResult> Index()
		{
			//
			// Retrieve the user's name, tenantID, and access token since they are parameters used to query the Graph API.
			//
			UserProfile profile;
			string accessToken = null;
			string tenantId = ClaimsPrincipal.Current.FindFirst(TenantIdClaimType).Value;
			if (tenantId != null)
			{
				accessToken = TokenCacheUtils.GetAccessTokenFromCacheOrRefreshToken(tenantId, _graphResourceId);
			}

			//
			// If the user doesn't have an access token, they need to re-authorize.
			//
			if (accessToken == null)
			{
				//
				// If refresh is set to true, the user has clicked the link to be authorized again.
				//
				if (Request.QueryString["reauth"] == "True")
				{
					//
					// Send an OpenID Connect sign-in request to get a new set of tokens.
					// If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
					// The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
					//
					HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
				}

				//
				// The user needs to re-authorize.  Show them a message to that effect.
				//
				profile = new UserProfile { DisplayName = " ", GivenName = " ", Surname = " " };
				ViewBag.ErrorMessage = "AuthorizationRequired";

				return View(profile);
			}

			//
			// Call the Graph API and retrieve the user's profile.
			//
			var requestUrl = String.Format(
				CultureInfo.InvariantCulture,
				_graphUserUrl,
				HttpUtility.UrlEncode(tenantId));
			var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			var response = await client.SendAsync(request);

			//
			// Return the user's profile in the view.
			//
			if (response.IsSuccessStatusCode)
			{
				var responseString = await response.Content.ReadAsStringAsync();
				profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
			}
			else
			{
				//
				// If the call failed, then drop the current access token and show the user an error indicating they might need to sign-in again.
				//
				TokenCacheUtils.RemoveAccessTokenFromCache(_graphResourceId);

				profile = new UserProfile {DisplayName = " ", GivenName = " ", Surname = " "};
				ViewBag.ErrorMessage = "UnexpectedError";

			}

			return View(profile);
		}
	}
}