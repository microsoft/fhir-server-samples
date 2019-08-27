// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using OpenQA.Selenium;

namespace FhirDashboard.Tests.E2E.UIPages
{
    internal class SignInPage : BasePage
    {
        public const string ExpectedTitle = "Sign in to your account";

        public IWebElement InputTextUserName
        {
            get
            {
                return Driver.GetElement(By.Name("loginfmt"));
            }
        }

        public IWebElement InputTextPassword
        {
            get
            {
                return Driver.GetElement(By.Name("passwd"));
            }
        }

        public IWebElement ButtonNext
        {
            get
            {
                return Driver.GetElement(By.Id("idSIButton9"), true);
            }
        }
    }
}
