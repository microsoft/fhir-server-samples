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

            var scopes = new string[] { $"{_configuration["FhirServerAudience"].TrimEnd('/')}/.default" };
            try
            {
                var accessToken = await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(HttpContext, scopes);
                ViewData["token"] = accessToken;
                return View();
            }
            catch (MsalUiRequiredException ex)
            {
                AuthenticationProperties properties = AuthenticationPropertiesBuilder.BuildForIncrementalConsent(scopes, HttpContext, ex);
                return Challenge(properties);
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
    }
}
