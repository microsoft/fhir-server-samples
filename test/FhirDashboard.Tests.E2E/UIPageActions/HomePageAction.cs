// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FhirDashboard.Tests.E2E.UIPages;

namespace FhirDashboard.Tests.E2E.UIPageActions
{
    internal class HomePageAction
    {
        /// <summary>
        /// Validate the title of the page with the expected title
        /// </summary>
        public void ValidateTitle()
        {
            HomePage homePage = new HomePage();
            homePage.ValidateTitle();
        }
    }
}
