// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using FhirDashboard.Tests.E2E.Configurations;
using FhirDashboard.Tests.E2E.UIPages;
using OpenQA.Selenium;

namespace FhirDashboard.Tests.E2E.UIPageActions
{
    internal class SignInPageAction
    {
        private SignInPage signInPage = new SignInPage();

        private void ClickNext()
        {
            signInPage.ButtonNext.Click();
        }

        private void InputUserName(string userName)
        {
            signInPage.InputTextUserName.SendKeys(userName);
        }

        private void InputPassword(string password)
        {
            signInPage.InputTextPassword.SendKeys(password);
        }

        /// <summary>
        /// Sign in from Microsoft sign in page
        /// </summary>
        /// <param name="userName">Microsoft user name</param>
        /// <param name="password">Password</param>
        public void SignIn(string userName, string password)
        {
            InputUserName(userName);
            ClickNext();
            InputPassword(password);

            // We need to wait for animation / button state change as next button has same id(idSIButton9)
            Thread.Sleep(2000);
            ClickNext();

            // Consent, should only be done if we can find the button
            try
            {
                IWebElement buttonIAccept = signInPage.ButtonNext;
                if (buttonIAccept != null)
                {
                    buttonIAccept.Click();
                }
            }
            catch (NoSuchElementException)
            {
                // Nothing to do
            }
        }

        /// <summary>
        /// Navigates to dashboard and sign in
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="password">Password</param>
        public void SignInandNavigateToDashBoard()
        {
            CommonActions.NavigateToDashBoard();
            var signInPageAction = new SignInPageAction();
            signInPageAction.SignIn(Configuration.DashboardUserUpn, Configuration.DashboardUserPassword);
        }
    }
}
