// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace FhirDashboard.Tests.E2E.UITestHelpers.Driver
{
    public class Driver : IWebDriver
    {
        public Driver(RemoteWebDriver remoteWebDriver)
        {
            RemoteWebDriver = remoteWebDriver;
        }

        public RemoteWebDriver RemoteWebDriver { get; set; }

        public void Close()
        {
            if (RemoteWebDriver != null)
            {
                RemoteWebDriver.Quit();
            }
        }

        public void Navigate(string url)
        {
            RemoteWebDriver.Url = url;
            RemoteWebDriver.Navigate();
        }

        public IWebElement GetElement(By by, bool clickable = false)
        {
            IWebElement webElement = null;
            var wait = new WebDriverWait(RemoteWebDriver, TimeSpan.FromSeconds(3));
            try
            {
                webElement = clickable ?
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(by)) :
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(by));
            }
            catch
            {
                // Do nothing
            }

            return webElement;
        }

        public ReadOnlyCollection<IWebElement> GetElements(By by)
        {
            ReadOnlyCollection<IWebElement> webElements = null;
            var wait = new WebDriverWait(RemoteWebDriver, TimeSpan.FromSeconds(20));
            try
            {
                webElements = wait.Until(driver => driver.FindElements(by));
            }
            catch
            {
                // Do nothing
            }

            return webElements;
        }

        public string Title()
        {
            return RemoteWebDriver.Title;
        }

        public void Maximize()
        {
            RemoteWebDriver.Manage().Window.Maximize();
        }

        public IOptions Manage()
        {
            return RemoteWebDriver.Manage();
        }

        public string URL()
        {
            return RemoteWebDriver.Url;
        }

        public void CloseBrowser()
        {
            RemoteWebDriver.Close();
        }
    }
}
