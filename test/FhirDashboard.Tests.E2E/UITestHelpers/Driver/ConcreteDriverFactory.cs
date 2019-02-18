// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using OpenQA.Selenium.Chrome;

namespace FhirDashboard.Tests.E2E.UITestHelpers.Driver
{
    internal class ConcreteDriverFactory : DriverFactory
    {
        public override IWebDriver GetWebDriver(string browser)
        {
            IWebDriver driver;

            switch (browser.ToLower())
            {
                // We can add more drivers here

                case "chrome":
                default:
                    var options = new ChromeOptions();

                    // We can add Config file for driver option. For the time being, I have hardcoded it.
                    options.AddArgument("--headless");
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--incognito");

                    var chromeDriver = new ChromeDriver(Environment.CurrentDirectory, options);
                    driver = new Driver(chromeDriver);
                    break;
            }

            driver.Maximize();
            return driver;
        }
    }
}
