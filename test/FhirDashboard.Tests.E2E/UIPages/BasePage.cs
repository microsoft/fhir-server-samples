// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FhirDashboard.Tests.E2E.UITestHelpers.Driver;
using Xunit;

namespace FhirDashboard.Tests.E2E.UIPages
{
    internal class BasePage
    {
        protected IWebDriver Driver
        {
            get
            {
                return WebDriver.CurrentState;
            }
        }

        public string Title
        {
            get { return Driver.Title(); }
        }

        public void OpenPage(string uRL)
        {
            Driver.Navigate(uRL);
        }

        public void ValidateTitle(string expectedTitle)
        {
            Assert.Equal(expectedTitle, Title);
        }
    }
}
