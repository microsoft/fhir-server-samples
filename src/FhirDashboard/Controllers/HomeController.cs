// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FhirDashboard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FhirDashboard.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IConfiguration _configuration;
        private ITokenAcquisition _tokenAcquisition;

        public HomeController(IConfiguration config, ITokenAcquisition tokenAcquisition)
        {
            _configuration = config;
            _tokenAcquisition = tokenAcquisition;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AboutMe()
        {
            var identity = User.Identity as ClaimsIdentity; // Azure AD V2 endpoint specific
            string preferred_username = identity.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            ViewData["FhirServerUrl"] = _configuration["FhirServerUrl"];
            ViewData["UPN"] = preferred_username;

            var scopes = new string[] { $"{_configuration["FhirServerUrl"].TrimEnd('/')}/.default" };
            try
            {
                var accessToken = await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(HttpContext, scopes);
                ViewData["token"] = accessToken;
                return View();
            }
            catch (MsalUiRequiredException ex)
            {
                if (CanbeSolvedByReSignInUser(ex))
                {
                    AuthenticationProperties properties = BuildAuthenticationPropertiesForIncrementalConsent(scopes, ex);
                    return Challenge(properties);
                }
                else
                {
                    throw;
                }
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Build Authentication properties needed for an incremental consent.
        /// </summary>
        /// <param name="scopes">Scopes to request</param>
        /// <returns>AuthenticationProperties</returns>
        private AuthenticationProperties BuildAuthenticationPropertiesForIncrementalConsent(string[] scopes, MsalUiRequiredException ex)
        {
            AuthenticationProperties properties = new AuthenticationProperties();

            // Set the scopes, including the scopes that ADAL.NET / MASL.NET need for the Token cache
            string[] additionalBuildInScopes = new string[] { "openid", "offline_access", "profile" };
            properties.SetParameter<ICollection<string>>(OpenIdConnectParameterNames.Scope, scopes.Union(additionalBuildInScopes).ToList());

            // Attempts to set the login_hint to avoid the logged-in user to be presented with an account selection dialog
            string loginHint = HttpContext.User.GetLoginHint();
            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                properties.SetParameter<string>(OpenIdConnectParameterNames.LoginHint, loginHint);

                string domainHint = HttpContext.User.GetDomainHint();
                properties.SetParameter<string>(OpenIdConnectParameterNames.DomainHint, domainHint);
            }

            // Additional claims required (for instance MFA)
            if (!string.IsNullOrEmpty(ex.Claims))
            {
                properties.Items.Add("claims", ex.Claims);
            }

            return properties;
        }

        private static bool CanbeSolvedByReSignInUser(MsalUiRequiredException ex)
        {
            bool canbeSolvedByReSignInUser = true;

            // ex.ErrorCode != MsalUiRequiredException.UserNullError indicates a cache problem
            // as when calling Contact we should have an
            // authenticate user (see the [Authenticate] attribute on the controller, but
            // and therefore its account should be in the cache
            // In the case of an InMemoryCache, this can happen if the server was restarted
            // as the cache is in the server memory

            return canbeSolvedByReSignInUser;
        }
    }
}
