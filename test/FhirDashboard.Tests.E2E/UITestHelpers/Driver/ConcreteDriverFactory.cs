// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
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

                    // We can add Config file for driver option. For the time being, I have hard coded it.
                    options.AddArgument("--headless");
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--incognito");

                    // VSTS Hosted agents set the ChromeWebDriver Env, locally that is not the case
                    // https://docs.microsoft.com/en-us/azure/devops/pipelines/test/continuous-test-selenium?view=vsts#decide-how-you-will-deploy-and-test-your-app

                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ChromeWebDriver")))
                    {
                        Environment.SetEnvironmentVariable("ChromeWebDriver", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    }

                    var chromeDriver = new ChromeDriver(Environment.GetEnvironmentVariable("ChromeWebDriver"), options);
                    driver = new Driver(chromeDriver);
                    break;
            }

            driver.Maximize();
            return driver;
        }
    }
}
