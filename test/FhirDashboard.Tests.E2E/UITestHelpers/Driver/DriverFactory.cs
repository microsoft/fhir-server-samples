// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FhirDashboard.Tests.E2E.UITestHelpers.Driver
{
    internal abstract class DriverFactory
    {
        public abstract IWebDriver GetWebDriver(string browser);
    }
}
