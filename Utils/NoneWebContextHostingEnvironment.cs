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
using System.Text;
using System.Web.Hosting;

namespace EPiServer.Business.Commerce.Tools.ImportAsset.Util
{
    public class NoneWebContextHostingEnvironment : IHostingEnvironment
    {
        VirtualPathProvider provider = null;
        public void RegisterVirtualPathProvider(VirtualPathProvider virtualPathProvider)
        {
            // Sets up the provider chain
            FieldInfo previousField = typeof(VirtualPathProvider).GetField("_previous", BindingFlags.NonPublic | BindingFlags.Instance);
            previousField.SetValue(virtualPathProvider, provider);
            provider = virtualPathProvider;
        }

        public System.Web.Hosting.VirtualPathProvider VirtualPathProvider
        {
            get { return provider; }
        }
        public string ApplicationID { get; set; }
        public string ApplicationPhysicalPath { get; set; }
        public string ApplicationVirtualPath { get; set; }
        public string MapPath(string virtualPath)
        {
            return Path.Combine(Environment.CurrentDirectory, virtualPath.Trim(' ', '~', '/').Replace('/', '\\'));
        }
    }

}
