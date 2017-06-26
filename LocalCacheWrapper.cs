using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Framework.Cache;

namespace EPiServer.Business.Commerce.Tools.ImportAsset
{
    /// <summary>
    /// ISynchronizedObjectInstanceCache implementation wrapping EPiServer.Framework.Cache.HttpRuntimeCache.
    /// Copied from solution to dependency in problem in CatalogImport's Program.cs;119502.
    /// </summary>
    internal class LocalCacheWrapper : HttpRuntimeCache, ISynchronizedObjectInstanceCache
    {
        public IObjectInstanceCache ObjectInstanceCache
        {
            get { return this; }
        }

        public void RemoveLocal(string key)
        {
            Remove(key);
        }

        public void RemoveRemote(string key)
        {
        }

        public FailureRecoveryAction SynchronizationFailedStrategy
        {
            get;
            set;
        }
    }
}
