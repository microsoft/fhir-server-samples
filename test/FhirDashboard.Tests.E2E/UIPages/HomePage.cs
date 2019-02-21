// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace FhirDashboard.Tests.E2E.UIPages
{
    internal class HomePage : BasePage
    {
        public const string ExpectedTitle = "Home Page - FhirDashboard";

        public void ValidateTitle()
        {
            ValidateTitle(ExpectedTitle);
        }
    }
}
