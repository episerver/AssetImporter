using EPiServer;
using EPiServer.Configuration;
using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using EPiServer.Web.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;


namespace EPiServer.Business.Commerce.Tools.ImportAsset.Util
{
    public class RuntimeProxyWrapper : IDisposable
    {
        protected AppDomain Domain { get; set; }
        private SiteProxy _siteProxyInstance;

        public RuntimeProxyWrapper(string destinationPath)
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = destinationPath;
            appDomainSetup.ConfigurationFile = Path.Combine(destinationPath,  "web.config");
            appDomainSetup.PrivateBinPath = "bin";
            System.IO.Directory.SetCurrentDirectory(destinationPath);
            Domain = AppDomain.CreateDomain("AppDomain:" + destinationPath, null, appDomainSetup);
            Domain.SetData(".appDomain", "AppDomain:" + destinationPath);
            Domain.SetData(".appVPath", "/");
            Domain.SetData(".appPath", destinationPath);
        }

        public SiteProxy GetSiteProxy()
        {
            if (_siteProxyInstance == null)
            {
                Type proxyType = typeof(SiteProxy);
                _siteProxyInstance = (SiteProxy)Domain.CreateInstanceFrom(proxyType.Assembly.Location, proxyType.FullName).Unwrap();
            }
            return _siteProxyInstance;
        }

        public void Dispose()
        {
            _siteProxyInstance = null;
            if (Domain != null)
            {
                AppDomain.Unload(Domain);
                Domain = null;
            }
        }
    }
}
