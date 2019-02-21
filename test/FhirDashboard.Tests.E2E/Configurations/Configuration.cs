// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace FhirDashboard.Tests.E2E.Configurations
{
    internal static class Configuration
    {
        private static IConfiguration _config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<FhirDashboard.Tests.E2E.FhirDashboardTests>()
                .Build();

        public static string FhirServerUrl
        {
            get
            {
                return _config["FhirServerUrl"];
            }
        }

        public static string DashboardUrl
        {
            get
            {
                return _config["DashboardUrl"];
            }
        }

        public static string DashboardUserUpn
        {
            get
            {
                return _config["DashboardUserUpn"];
            }
        }

        public static string DashboardUserPassword
        {
            get
            {
                return _config["DashboardUserPassword"];
            }
        }
    }
}
