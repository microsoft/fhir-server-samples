// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FhirDashboard.Tests.E2E.Configurations;
using FhirDashboard.Tests.E2E.UITestHelpers.Driver;
using OpenQA.Selenium;

namespace FhirDashboard.Tests.E2E.UIPageActions
{
    public static class CommonActions
    {
        /// <summary>
        /// Click on next button which is common to the concent pages
        /// </summary>
        public static void ClickNext()
        {
            IWebElement acceptButton = WebDriver.CurrentState.GetElement(By.Id("idSIButton9"));
            acceptButton.Click();
            Thread.Sleep(2000);
        }

        /// <summary>
        /// Navigates to about me page
        /// </summary>
        public static void NavigateToAboutMe()
        {
            string dashboardUrl = Configuration.DashboardUrl;
            WebDriver.CurrentState.Navigate($"{dashboardUrl}/Home/AboutMe");

            if (!WebDriver.CurrentState.URL().StartsWith($"{dashboardUrl}/Home/AboutMe"))
            {
                // We may have to consent a second time since we are asking for a new audience
                try
                {
                    ClickNext();
                }
                catch
                {
                    // Nothing to do
                }
            }

            var aboutMePageAction = new AboutMePageAction();
            aboutMePageAction.ValidateTitle();
        }

        /// <summary>
        /// Navigates to dashboard
        /// </summary>
        public static void NavigateToDashBoard()
        {
            WebDriver.CurrentState.Navigate(Configuration.DashboardUrl);
        }

        /// <summary>
        /// Navigates to dashboard and sign in
        /// </summary>
        /// <param name="userName">username</param>
        /// <param name="password">password</param>
        public static void SignInandNavigateToDashBoard()
        {
            NavigateToDashBoard();
            var signInPageAction = new SignInPageAction();
            signInPageAction.SignIn(Configuration.DashboardUserUpn, Configuration.DashboardUserPassword);
        }

        /// <summary>
        /// Checks whether the site is up
        /// </summary>
        /// <param name="url">site to check</param>
        /// <returns>return result</returns>
        public static async Task<HttpResponseMessage> CheckForSiteSuccess(string url)
        {
            var client = new HttpClient();

            // We have to make sure the website is up
            var result = await client.GetAsync(url);
            int waitCount = 0;
            while ((waitCount++ < 10) && !result.IsSuccessStatusCode)
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                result = await client.GetAsync(url);
            }

            return result;
        }
    }
}
