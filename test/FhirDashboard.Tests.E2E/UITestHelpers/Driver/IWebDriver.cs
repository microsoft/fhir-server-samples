// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace FhirDashboard.Tests.E2E.UITestHelpers.Driver
{
    public interface IWebDriver
    {
        string URL();

        string Title();

        void Navigate(string url);

        void Close();

        IWebElement GetElement(By by, bool clickable = false);

        ReadOnlyCollection<IWebElement> GetElements(By by);

        void Maximize();

        IOptions Manage();

        void CloseBrowser();
    }
}
