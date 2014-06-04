using System;
using System.Web;

using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using DemoAAD.Utils;

namespace DemoAAD
{
    public partial class Startup
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used to authenticate the application to Azure AD.  Azure AD supports password and certificate credentials.
        // The Metadata Address is used by the application to retrieve the signing keys used by Azure AD.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        //
        private static readonly string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static readonly string AppKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static readonly string AadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static readonly string Tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static readonly string PostLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

	    readonly string _authority = String.Format(CultureInfo.InvariantCulture, AadInstance, Tenant);

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
	    private const string GraphResourceId = "https://graph.windows.net";

	    public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    Client_Id = ClientId,
                    Authority = _authority,
                    Post_Logout_Redirect_Uri = PostLogoutRedirectUri,

                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        //
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        //
                        AccessCodeReceived = context =>
                        {
                            var code = context.Code;

                            var credential = new ClientCredential(ClientId, AppKey);
                            var authContext = new AuthenticationContext(_authority);
                         
                            var result = authContext.AcquireTokenByAuthorizationCode(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, GraphResourceId);

                            // Cache the access token and refresh token
                            TokenCacheUtils.SaveAccessTokenInCache(GraphResourceId, result.AccessToken, (result.ExpiresOn.AddMinutes(-5)).ToString());
                            TokenCacheUtils.SaveRefreshTokenInCache(result.RefreshToken);

                            return Task.FromResult(0);
                        }

                    }

                });
        }
    }
}