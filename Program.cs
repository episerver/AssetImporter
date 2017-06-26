using EPiServer.Events.ChangeNotification;
using EPiServer.Events.ChangeNotification.Implementation;
using EPiServer.Framework.Cache;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.ServiceLocation.Internal;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce;
using Mediachase.Commerce.Assets;
using Mediachase.Commerce.Assets.Import;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Core.Dto;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Inventory;
using Mediachase.Commerce.Inventory.Database;
using Mediachase.Commerce.Pricing;
using Mediachase.Commerce.Pricing.Database;
using Mediachase.Commerce.Security;
using Mediachase.Data.Provider;
using Mediachase.MetaDataPlus;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace EPiServer.Business.Commerce.Tools.ImportAsset
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            InitializeServiceLocator();

            ConsoleLog("Asset Importer v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " Copyright 2011-2012 EPiServer AB\n");

            string csvFile = null;
            string assetFolder = null;
            string importFolder = "Catalogs";
            string sitePath = null;

            switch (args.Length)
            {
                case 0:
                    DisplayUsage();
                    return 0;
                case 1:
                    ConsoleLog("Arguments incorrect.");
                    return 1;
                case 2:
                    ConsoleLog("Arguments incorrect.");
                    return 2;
                default:
                    csvFile = args[0];
                    assetFolder = args[1];
                    sitePath = args[2];
                    break;
            }

            bool importByAsset = false;
            bool uselegacy = false;
            bool mappingOnly = false;

            for (int i = 3; i < args.Length; i++)
            {
                if (i == 3 && args[i][0] != '-')
                {
                    importFolder = args[i];
                    continue;
                }
                switch (args[i].ToLower())
                {
                    case "-byasset":
                        importByAsset = true;
                        break;
                    case "-uselegacy":
                        uselegacy = true;
                        break;
                    case "-mappingonly":
                        mappingOnly = true;
                        break;
                    case "-verbose":
                        break;
                    default:
                        ConsoleLogFormat("Invalid command line option {0}.", args[i]);
                        DisplayUsage();
                        return 1;
                }
            }

            var stopwatch = Stopwatch.StartNew();

            ConsoleLog("Reading mappings from " + csvFile);
            ConsoleLog("Importing assets from " + assetFolder);
            ConsoleLog("Importing assets to   " + importFolder);
            ConsoleLog("Site Path   " + sitePath);

            ImportBase importer = null;
            if (!uselegacy)
            {
                if (!mappingOnly)
                {
                    importer = new ImportByCmsContent(assetFolder, importFolder, sitePath);
                }
                else
                {
                    importer = new CmsContentMapping(ServiceLocator.Current.GetInstance<IConnectionStringHandler>());
                }
            }
            else
            {
                InitAssetDatabase();
                InitECFAppContext();
                if (importByAsset)
                {
                    importer = new ImportByAsset(assetFolder, importFolder);
                }
                else
                {
                    importer = new ImportByProduct(assetFolder, importFolder);
                }
            }
            var reader = new CsvReader(csvFile);
            importer.DoImport(reader.Entries());
            stopwatch.Stop();
            ConsoleLog("\nElapsed time " + stopwatch.Elapsed.ToString());
            return 0;
        }

        private static string ConsoleLog(object o)
        {
            string s = o.ToString();
            Console.WriteLine(s);
            return s;
        }

        private static string ConsoleLogFormat(string format, params object[] args)
        {
            string s = string.Format(format, args);
            Console.WriteLine(s);
            return s;
        }

        private static void InitAssetDatabase()
        {
            DataContext.Current = new DataContext(ConfigurationManager.ConnectionStrings["EcfSqlConnection"].ConnectionString);
            FolderEntity.ListRootFolders();
        }

        private static void InitECFAppContext()
        {
            AppDto dto = AppContext.Current.GetApplicationDto();
            // If application does not exists or is not active, prevent login
            if (dto == null || dto.Application.Count == 0 || !dto.Application[0].IsActive)
            {
                return;
            }

            AppContext.Current.ApplicationId = dto.Application[0].ApplicationId;
            AppContext.Current.ApplicationName = dto.Application[0].Name;

            ConsoleLogFormat("AppContext.Current.ApplicationId = {0}", AppContext.Current.ApplicationId);
            ConsoleLogFormat("AppContext.Current.ApplicationName = {0}", AppContext.Current.ApplicationName);
        }

        private static void DisplayUsage()
        {
            ConsoleLog("\nUsage: AssetImporter csvfile assetfolder sitepath [assetroot] [-uselegacy] [-byasset] [-reusefile] [-verbose] \n");
            ConsoleLog("\tcsvfile\t\tA CSV file that contains mapping between products/variations and assets.");
            ConsoleLog("\tassetfolder\tA folder that contains the assets to be imported.");
            ConsoleLog("\tsitepath\tThe folder that contains CMS website.");
            ConsoleLog("\tassetroot\tThe name of the asset folder where the assets will be imported.");
            ConsoleLog("\t-uselegacy\tAsset use ECF asset system.");
            ConsoleLog("\t-mappingonly\tMapping between Catalog Assets and CMS Content Data but won't import asset file.");
            ConsoleLog("\t-byasset\tIndicates that the file structure from assetfolder will be kept intact for the import. The default\n" + 
                       "\t\t\twhen importing is to create a structure with the products, the groups and finally the assets.");
            ConsoleLog("\t-verbose\tEnable detailed logging of the asset import process.");

            ConsoleLog("\nMore information about the asset folder\n");
            ConsoleLog("The top level of the asset folder should be the asset groups. If importing images this is usually the different image\n" + 
                       "sizes (Small, Medium and Large for example). In the asset group folders you will have the individual assets, named as\n" + 
                       "in the csv file.\n");

            ConsoleLog("More information about the CSV file\n");
            ConsoleLog("The first row of the file contains headers. If the first header contains the word 'asset', the contents of the first\n" + 
                       "column is interpreted as the asset file name. The other column is the SKU ID / product code / variation code that the\n" + 
                       "asset will be associated with. Please note that when you have multiple groups in the asset folder you only need to\n" + 
                       "have one line in the CSV file, the import will automatically import all files with the same name from the existing\n" + 
                       "asset groups.\n");
        }

        /// <summary>
        /// Initializes the service locator.
        /// </summary>
        static void InitializeServiceLocator()
        {
            var container = new StructureMap.Container();
            var locator = new StructureMapServiceLocator(container);
            var context = new ServiceConfigurationContext(HostType.Installer, new StructureMapConfiguration(container));

            context.Services.AddSingleton<IRequiredMetaFieldCollection, NoRequiredMetaFields>();
            context.Services.AddSingleton<IWarehouseRepository, WarehouseRepositoryDatabase>();
            context.Services.AddSingleton<ISynchronizedObjectInstanceCache, LocalCacheWrapper>();
            context.Services.AddSingleton<ICatalogSystem>(locate => CatalogContext.Current);
            context.Services.AddSingleton<IChangeNotificationManager, NullChangeNotificationManager>();
            context.Services.AddSingleton<IPriceService, PriceServiceDatabase>();

            context.Services.AddTransient<SecurityContext>(locate => SecurityContext.Current);
            context.Services.AddTransient<CustomerContext>(locate => CustomerContext.Current);
            context.Services.AddTransient<FrameworkContext>(locate => FrameworkContext.Current);

            ServiceLocator.SetLocator(locator);
        }
    }
}
