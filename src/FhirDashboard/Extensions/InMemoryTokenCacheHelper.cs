// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.AspNetCore.Authentication
{
    public class InMemoryTokenCacheHelper
    {
        private string _userId = string.Empty;
        private string _cacheId = string.Empty;
        private IMemoryCache _memoryCache;
        private TokenCache _cache = new TokenCache();

        public InMemoryTokenCacheHelper(string userId, HttpContext httpcontext, IMemoryCache aspnetInMemoryCache)
        {
            // not object, we want the SUB
            _userId = userId;
            _cacheId = _userId + "_TokenCache";
            _memoryCache = aspnetInMemoryCache;
            Load();
        }

        public TokenCache GetMsalCacheInstance()
        {
            _cache.SetBeforeAccess(BeforeAccessNotification);
            _cache.SetAfterAccess(AfterAccessNotification);
            Load();
            return _cache;
        }

        public void Load()
        {
            byte[] blob;
            if (_memoryCache.TryGetValue(_cacheId, out blob))
            {
                _cache.Deserialize(blob);
            }
        }

        public void Persist()
        {
            // Reflect changes in the persistent store
            byte[] blob = _cache.Serialize();
            _memoryCache.Set(_cacheId, blob);
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
