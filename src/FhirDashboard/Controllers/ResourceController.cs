// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace FhirDashboard.Controllers
{
    public class ResourceController : Controller
    {
        private IConfiguration _configuration;
        private ITokenAcquisition _tokenAcquisition;

        public ResourceController(IConfiguration config, ITokenAcquisition tokenAcquisition)
        {
            _configuration = config;
            _tokenAcquisition = tokenAcquisition;
        }

        [HttpGet("/Resource/{resourceType}/{resourceId}")]
        public async Task<IActionResult> GetAction(string resourceType, string resourceId)
        {
            var scopes = new string[] { $"{_configuration["FhirImportService:Audience"].TrimEnd('/')}/.default" };
            string accessToken;
            try
            {
                accessToken = await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(HttpContext, scopes);
            }
            catch (MsalUiRequiredException ex)
            {
                AuthenticationProperties properties = AuthenticationPropertiesBuilder.BuildForIncrementalConsent(scopes, HttpContext, ex);
                return Challenge(properties);
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_configuration["FhirServerUrl"]);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                HttpResponseMessage result = await client.GetAsync($"/{resourceType}/{resourceId}");
                result.EnsureSuccessStatusCode();

                ViewData["ResourceJson"] = await result.Content.ReadAsStringAsync();
            }

            return View("Index");
        }
    }
}