// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationPropertiesBuilder
    {
        public static AuthenticationProperties BuildForIncrementalConsent(string[] scopes, HttpContext httpContext, MsalUiRequiredException ex)
        {
            AuthenticationProperties properties = new AuthenticationProperties();

            // Set the scopes, including the scopes that ADAL.NET / MASL.NET need for the Token cache
            string[] additionalBuildInScopes = new string[] { "openid", "offline_access", "profile" };
            properties.SetParameter<ICollection<string>>(OpenIdConnectParameterNames.Scope, scopes.Union(additionalBuildInScopes).ToList());

            // Attempts to set the login_hint to avoid the logged-in user to be presented with an account selection dialog
            string loginHint = httpContext.User.GetLoginHint();
            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                properties.SetParameter<string>(OpenIdConnectParameterNames.LoginHint, loginHint);

                string domainHint = httpContext.User.GetDomainHint();
                properties.SetParameter<string>(OpenIdConnectParameterNames.DomainHint, domainHint);
            }

            // Additional claims required (for instance MFA)
            if (!string.IsNullOrEmpty(ex.Claims))
            {
                properties.Items.Add("claims", ex.Claims);
            }

            return properties;
        }
    }
}
