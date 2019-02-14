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

            // Config file can be added for the browser selection for now have hardcoded it
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
