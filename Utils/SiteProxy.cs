using EPiServer;
using EPiServer.Configuration;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Framework.Configuration;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Hosting;
using EPiServer.Shell.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Hosting;
using Mediachase.Commerce.Assets.Import;

namespace EPiServer.Business.Commerce.Tools.ImportAsset.Util
{

    public class SiteProxy : MarshalByRefObject
    {
        private void InitalizeEPiServer(string webConfigPath)
        {
            if (!File.Exists(webConfigPath))
            {
                throw new ArgumentException("The configuration file '" + webConfigPath + "' does not exist", "webConfigPath");
            }

            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = webConfigPath };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            EPiServerFrameworkSection frameworkSection = config.GetSection("episerver.framework") as EPiServerFrameworkSection;

            EPiServerShellSection shellSection = config.GetSection("episerver.shell") as EPiServerShellSection;
            foreach (ModuleElement s in shellSection.ProtectedModules)
            {
                string resourcePath = s.ResourcePath.Replace("{rootpath}", shellSection.ProtectedModules.RootPath).Replace("{modulename}", s.Name);
                var fallbackMapPathVPPConfig = new System.Collections.Specialized.NameValueCollection();
                fallbackMapPathVPPConfig.Add("physicalPath", AppDomain.CurrentDomain.BaseDirectory);
                fallbackMapPathVPPConfig.Add("virtualPath", resourcePath);
                VirtualPathNonUnifiedProvider fallbackVPP = new VirtualPathNonUnifiedProvider("fallbackMapPathVPP", fallbackMapPathVPPConfig);
                GenericHostingEnvironment.Instance.RegisterVirtualPathProvider(fallbackVPP);
            }

            ConfigurationSource.Instance = new FileConfigurationSource(config);

            EPiServer.Framework.Initialization.InitializationModule.FrameworkInitialization(HostType.Installer);
        }
        private string InitializeHostingEnvironment(string destinationPath, string virtualDirectory)
        {
            if (string.IsNullOrEmpty(destinationPath)) throw new NullReferenceException("destinationPath");
            if (!Directory.Exists(destinationPath)) throw new ArgumentException("The directory '" + destinationPath + "' does not exist.", "destinationPath");

            if (string.IsNullOrEmpty(virtualDirectory))
            {
                virtualDirectory = "/";
            }


            var noneWebContextHostingEnvironment = new NoneWebContextHostingEnvironment();
            noneWebContextHostingEnvironment.ApplicationVirtualPath = virtualDirectory;// : virtualDirectory.Insert(0, "/");
            noneWebContextHostingEnvironment.ApplicationPhysicalPath = destinationPath;
            GenericHostingEnvironment.Instance = noneWebContextHostingEnvironment;

            var fallbackMapPathVPPConfig = new System.Collections.Specialized.NameValueCollection();
            fallbackMapPathVPPConfig.Add("physicalPath", AppDomain.CurrentDomain.BaseDirectory);
            fallbackMapPathVPPConfig.Add("virtualPath", "~/");
            VirtualPathNonUnifiedProvider fallbackVPP = new VirtualPathNonUnifiedProvider("fallbackMapPathVPP", fallbackMapPathVPPConfig);
            GenericHostingEnvironment.Instance.RegisterVirtualPathProvider(fallbackVPP);

            Global.BaseDirectory = destinationPath;
            return virtualDirectory;
        }

        public SiteProxy()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public void ImportAsset(IEnumerable<AssetMapping> entries, string assetFolder, string importFolder)
        {
            CmsContentAssetImporter importer = new CmsContentAssetImporter(entries, assetFolder,  importFolder);
            importer.ImportAsset(); 
        }

        public IEnumerable<String> GetRegiteredFileData()
        {
            var filedatas = ServiceLocator.Current.GetInstance<IContentTypeRepository>().List().Where(ct => typeof(MediaData).IsAssignableFrom(ct.ModelType));
            List<String> res = new List<string>();
            foreach (var fileData in filedatas)
            {
                res.Add(fileData.ModelType.FullName);
            }
            return res;
        }
        public IEnumerable<String> GetLanguages()
        {
            return ServiceLocator.Current.GetInstance<ILanguageBranchRepository>().ListEnabled().Select(b => b.Culture.Name).ToList();
        }
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Initialize(string destinationPath, string virtualDirectory)
        {
            virtualDirectory = InitializeHostingEnvironment(destinationPath, virtualDirectory);

            // Do not do this in the constructor as it can throw causing the dispose pattern to fail on instances of this object
            string webConfigPath = Path.Combine(destinationPath, "web.config");
            InitalizeEPiServer(webConfigPath);
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().IsSameAssemblyName(assemblyName)).FirstOrDefault();

            if (assembly == null && Global.BaseDirectory != null)
            {
                string assemblyPath = Path.Combine(Global.BaseDirectory, "bin", assemblyName.Name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    assembly = Assembly.LoadFile(assemblyPath);
                }
            }

            return assembly;
        }

    }

}
