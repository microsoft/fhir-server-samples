// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FhirServerSamples.FhirImportService
{
    public static class FhirImportServiceExtension
    {
        /// <summary>
        /// Adds FHIR Import Service
        /// </summary>
        /// <param name="services">The services collection.</param>
        public static IServiceCollection AddFhirImportService(
            this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddHostedService<FhirImportService>();
            return services;
        }
    }
}
