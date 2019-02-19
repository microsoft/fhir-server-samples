// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using FhirDashboard.Tests.E2E.UITestHelpers.Driver;

namespace FhirDashboard.Tests.E2E
{
    public class BaseTest : IDisposable
    {
        public BaseTest()
        {
            DriverFactory driverFactory = new ConcreteDriverFactory();

            // We can add Config file to select browser. For the time being, I have hard coded it.
            Driver = driverFactory.GetWebDriver("chrome");
        }

        protected IWebDriver Driver
        {
            get
            {
                return WebDriver.CurrentState;
            }

            set
            {
                WebDriver.CurrentState = value;
            }
        }

        public void Dispose()
        {
            Driver.Close();
        }
    }
}
