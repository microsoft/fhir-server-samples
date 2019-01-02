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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.AspNetCore.Authentication
{
    public class SessionTokenCacheHelper
    {
        private static ReaderWriterLockSlim _sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private string _userId = string.Empty;
        private string _cacheId = string.Empty;
        private ISession _session;

        private TokenCache _cache = new TokenCache();

        public SessionTokenCacheHelper(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            _userId = userId;
            _cacheId = _userId + "_TokenCache";
            _session = httpcontext.Session;
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
            _session.LoadAsync().Wait();

            _sessionLock.EnterReadLock();
            try
            {
                byte[] blob;
                if (_session.TryGetValue(_cacheId, out blob))
                {
                    Debug.WriteLine($"INFO: Deserializing session {_session.Id}, cacheId {_cacheId}");
                    _cache.Deserialize(blob);
                }
                else
                {
                    Debug.WriteLine($"INFO: cacheId {_cacheId} not found in session {_session.Id}");
                }
            }
            finally
            {
                _sessionLock.ExitReadLock();
            }
        }

        public void Persist()
        {
            _sessionLock.EnterWriteLock();

            try
            {
                Debug.WriteLine($"INFO: Serializing session {_session.Id}, cacheId {_cacheId}");

                // Reflect changes in the persistent store
                byte[] blob = _cache.Serialize();
                _session.Set(_cacheId, blob);
                _session.CommitAsync().Wait();
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
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
