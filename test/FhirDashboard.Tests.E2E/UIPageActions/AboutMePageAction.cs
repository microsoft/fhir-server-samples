// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FhirDashboard.Tests.E2E.UIPages;
using Xunit;

namespace FhirDashboard.Tests.E2E.UIPageActions
{
    internal class AboutMePageAction
    {
        private AboutMePage aboutMePage = new AboutMePage();

        /// <summary>
        /// Validate token
        /// </summary>
        /// <param name="serverUrl">Pass the server URL</param>
        public void ValidateToken(string serverUrl)
        {
            var element = aboutMePage.TextToken;
            string elementval = element.GetAttribute("value");
            var jwtHandler = new JwtSecurityTokenHandler();
            Assert.True(jwtHandler.CanReadToken(elementval), "Unable to read token !");

            var token = jwtHandler.ReadJwtToken(elementval);
            var aud = token.Claims.Where(c => c.Type == "aud");
            Assert.Single(aud);

            var tokenAudience = aud.First().Value;
            Assert.Equal(serverUrl, tokenAudience);
        }
    }
}
