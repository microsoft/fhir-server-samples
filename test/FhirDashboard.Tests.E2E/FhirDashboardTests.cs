// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using FhirDashboard.Tests.E2E.Configurations;
using FhirDashboard.Tests.E2E.UIPageActions;
using Xunit;

namespace FhirDashboard.Tests.E2E
{
    public class FhirDashboardTests : BaseTest
    {
        [Fact]
        public async Task DashboardLoginSuccessFull_and_TokenValidForFhirServer()
        {
            // Verify all environment variables are set
            Assert.True(!string.IsNullOrWhiteSpace(Configuration.FhirServerUrl), "Environment variable FhirServerUrl not set !");
            Assert.True(!string.IsNullOrWhiteSpace(Configuration.DashboardUrl), "Environment variable DashboardUrl not set !");
            Assert.True(!string.IsNullOrWhiteSpace(Configuration.DashboardUserUpn), "Environment variable DashboardUserUpn not set !");
            Assert.True(!string.IsNullOrWhiteSpace(Configuration.DashboardUserPassword), "Environment variable DashboardUserPassword not set !");

            // On a fresh deployment it can take time before site is deployed
            var result = await CommonActions.CheckForSiteSuccess(Configuration.DashboardUrl);
            Assert.True(result.IsSuccessStatusCode);

            // Hit dashboard URL and sign in using user name and password
            var signInPageAction = new SignInPageAction();
            signInPageAction.SignInandNavigateToDashBoard();

            // Navigate to about me page and validate token
            CommonActions.NavigateToAboutMe();
            var aboutMePageAction = new AboutMePageAction();
            aboutMePageAction.ValidateToken(Configuration.FhirServerUrl);
        }
    }
}
