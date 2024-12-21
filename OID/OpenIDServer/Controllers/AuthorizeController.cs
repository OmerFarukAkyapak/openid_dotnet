using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;
using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIDServer.Controllers
{
    public class AuthorizeController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        public AuthorizeController(IOpenIddictApplicationManager applicationManager)
        {
            _applicationManager = applicationManager;
        }

        [HttpGet("~/connect/authorize")]
        public IActionResult Authorize(string client_id, string response_type, string redirect_uri, string scope, string state)
        {
            TempData["ClientId"] = client_id;
            TempData["ResponseType"] = response_type;
            TempData["RedirectUri"] = redirect_uri;
            TempData["Scope"] = scope;
            TempData["State"] = state;

            if (User.Identity.IsAuthenticated)
            {

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Login", "Auth");
        }

        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> AuthorizePost(
            [FromForm] string client_id,
            [FromForm] string response_type,
            [FromForm] string redirect_uri,
            [FromForm] string scope,
            [FromForm] string state,
            [FromForm] string accept)
        {

            if (string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(redirect_uri) || string.IsNullOrEmpty(response_type))
            {
                return BadRequest("Required parameters are missing: client_id, redirect_uri or response_type");
            }

            TempData["ClientId"] = client_id;
            TempData["RedirectUri"] = redirect_uri;
            TempData["Scope"] = scope;
            TempData["State"] = state;

            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("OpenID Connect request could not be received");

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Challenge(
                    authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = $"{Request.PathBase}{Request.Path}{Request.QueryString}"
                    });
            }

            if (accept == "no")
            {
                return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (!User.Identity.IsAuthenticated)
            {
                return BadRequest(
                    new OpenIddictResponse
                    {
                        Error = Errors.AccessDenied,
                        ErrorDescription = "The resource owner or authorization server denied the request"
                    });
            }

            var application = await _applicationManager.FindByClientIdAsync(client_id) ?? throw new InvalidOperationException("This clientId was not found");

            var sub = result.Principal?.Identity?.Name ?? "open-id-api";

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            identity.AddClaim(new Claim(Claims.Subject, sub)
                     .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application)
                    ?? throw new InvalidOperationException("Application name not found"))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

            identity.AddClaim(new Claim(Claims.Audience, "OpenID")
                .SetDestinations(Destinations.AccessToken));

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(request.GetScopes());


            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }


        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest();


            if (request?.IsClientCredentialsGrantType() is not null)
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
                if (application is null)
                    throw new InvalidOperationException("This clientId was not found");

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                identity.AddClaim(new Claim(Claims.Subject, await _applicationManager.GetClientIdAsync(application)
                    ?? throw new InvalidOperationException("Client ID not found"))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

                identity.AddClaim(new Claim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application)
                    ?? throw new InvalidOperationException("Application name not found"))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

                identity.AddClaim(new Claim(Claims.Audience, "OpenID")
                    .SetDestinations(Destinations.AccessToken));

                var scopes = request.GetScopes();
                identity.AddClaim(new Claim(Claims.Scope, string.Join(" ", scopes))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

                var claimsPrincipal = new ClaimsPrincipal(identity);
                claimsPrincipal.SetScopes(request.GetScopes());

                return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request?.IsAuthorizationCodeGrantType() is not null)
            {
                var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            }

            throw new NotImplementedException("The specified grant type is not implemented");
        }

        [HttpPost("~/connect/logout")]
        public IActionResult Logout(string redirect_uri)
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (string.IsNullOrEmpty(redirect_uri))
            {
                redirect_uri = "/";
            }

            return Redirect(redirect_uri);
        }
    }
}
