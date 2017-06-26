using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using EPiServer.Business.Commerce.Tools.ImportAsset.Helper;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce.Assets.Import;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Data;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Storage;
using Mediachase.Data.Provider;
using MappingEntryHelper = Mediachase.Commerce.Assets.Import.MappingEntryHelper;

namespace EPiServer.Business.Commerce.Tools.ImportAsset
{
    public class CmsContentAssetImporter
    {
        private string _folderPath;
        private IEnumerable<AssetMapping> _entries;
        private string _importFolder;

        public CmsContentAssetImporter(IEnumerable<AssetMapping> entries, string folderPath, string importFolder)
        {
            _folderPath = folderPath; 
            _importFolder = importFolder;
            _entries = entries;
        }

        public void ImportAsset()
        {
            // Get mapping from CSV file 
            IEnumerable<AssetMapping> AssetsMapping = _entries;
            
            // Get image from Asset folder
            Console.WriteLine("Upload folder to CMS site... ");
            DirectoryInfo di = new DirectoryInfo(_folderPath);
            FileInfo[] assetFiles = di.GetFiles("*.bmp", SearchOption.AllDirectories)
                                .Union(di.GetFiles("*.jpg", SearchOption.AllDirectories))
                                .Union(di.GetFiles("*.png", SearchOption.AllDirectories))
                                        .ToArray(); 

            foreach (var assetFile in assetFiles)
            {
                string assetFileName = assetFile.Name;
                                
                #region Import asset file as CMS content
                // Create CMS file content and get Guid after import CMS
                var cmsContentGuid = ContentHelper.CreateFileContent(assetFile, _importFolder); 
                #endregion
                
                // when import to CMS done 
                if (AssetsMapping.Where(m => m.AssetName.Equals(assetFileName,StringComparison.OrdinalIgnoreCase)).Count() >0)
                {
                    foreach(var mapping in AssetsMapping.Where(m => m.AssetName.Equals(assetFileName,StringComparison.OrdinalIgnoreCase)))
                    {
                        mapping.AssetKey = cmsContentGuid.ToString();
                    }
                }
            }

            Console.WriteLine("Association asset with entry... ");
            foreach (var mapping in AssetsMapping)
            {
                MappingEntryHelper.AddMapping(mapping.ProductCode, mapping);
            }

           MappingEntryHelper.AssociateAssetsWithProduct();
           MappingEntryHelper.AssociateAssetsWithCatalogNode();

           //Console.WriteLine("Catalog Asset updated: " + updated);
        }

    }
}
