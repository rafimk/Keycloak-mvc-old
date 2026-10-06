using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

[assembly: OwinStartup(typeof(keycloak_sample.Startup))]

namespace keycloak_sample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.Always,
                CookieSameSite = SameSiteMode.Lax,
                CookieManager = new SystemWebCookieManager()
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType,
                SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                Authority = ConfigurationManager.AppSettings["Keycloak:Authority"],
                ClientId = ConfigurationManager.AppSettings["Keycloak:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["Keycloak:ClientSecret"],
                RedirectUri = ConfigurationManager.AppSettings["Keycloak:RedirectUri"],
                CallbackPath = new PathString("/signin-oidc"),
                ResponseType = "code",
                Scope = "openid profile email",
                RedeemCode = true,
                UsePkce = true,
                // The local Docker server uses HTTP. Enable HTTPS metadata in production.
                RequireHttpsMetadata = bool.Parse(ConfigurationManager.AppSettings["Keycloak:RequireHttpsMetadata"]),
                CookieManager = new SystemWebCookieManager(),
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    ValidateIssuer = true,
                    ValidateAudience = true
                }
            });
        }
    }
}
