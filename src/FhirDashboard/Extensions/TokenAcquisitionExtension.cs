// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication
{
        /// <summary>
    /// Extension class enabling adding the TokenAcquisition service
    /// </summary>
    public static class TokenAcquisitionExtension
    {
        /// <summary>
        /// Add the token acquisition service.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>the service collection</returns>
        /// <example>
        /// This method is typically called from the Startup.ConfigureServices(IServiceCollection services)
        /// Note that the implementation of the token cache can be chosen separately.
        ///
        /// <code>
        /// // Token acquisition service and its cache implementation as a session cache
        /// services.AddTokenAcquisition()
        /// .AddDistributedMemoryCache()
        /// .AddSession()
        /// .AddSessionBasedTokenCache()
        ///  ;
        /// </code>
        /// </example>
        public static IServiceCollection AddTokenAcquisition(this IServiceCollection services)
        {
            // Token acquisition service
            services.AddSingleton<ITokenAcquisition, TokenAcquisition>();
            return services;
        }
    }
}