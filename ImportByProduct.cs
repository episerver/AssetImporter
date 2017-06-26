using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Assets;
using Mediachase.Commerce.Assets.Import;
using Mediachase.Commerce.Catalog.Dto;

namespace EPiServer.Business.Commerce.Tools.ImportAsset
{
    /// <summary>
    /// An import implementation that will import assets according to the the structure of the products.
    /// </summary>
    /// <remarks>
    /// This import class should be used when each product has its own unique set of images, especially if each product has many assets associated with it.
    /// </remarks>
    public class ImportByProduct : ImportBase
    {
        private const int MAX_ITEM_PER_PACKAGE = 500;

        private int _subFolderCount;
        private string _packageName;
        private FolderEntity _packageFolder;
        private FolderEntity _productFolder;
        private List<FolderEntity> _assetGroupFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportByProduct"/> class.
        /// </summary>
        /// <param name="assetPath">The local file path where the files to import are present.</param>
        /// <param name="assetFolderName">Name of the asset folder that will be used/created in ECF.</param>
        public ImportByProduct(string assetPath, string assetFolderName)
            : base(assetPath, assetFolderName)
        {
            _assetGroupFolder = new List<FolderEntity>();
        }

        /// <summary>
        /// Does the import.
        /// </summary>
        /// <param name="entries">The product-to-asset mapping entries to import.</param>
        public override void DoImport(IEnumerable<AssetMapping> entries)
        {
            foreach (var assetEntries in entries.GroupBy(e => e.ProductCode))
            {
                using (var productDto = GetProductDto(assetEntries.First().ProductCode))
                {
                    if (productDto == null)
                    {
                        continue;
                    }

                    ResetFolders();
                    var assets = CreateAssetsForProduct(assetEntries, productDto);
                    AssociateAssetsWithProduct(assets, productDto);
                }
                _subFolderCount++;
            }
        }

        /// <summary>
        /// Creates the assets for product.
        /// </summary>
        /// <param name="assetEntries">The asset entries.</param>
        /// <param name="productDto">The product dto.</param>
        /// <returns>An enumeration of all assets for the specified product.</returns>
        private IEnumerable<AssetWrapper> CreateAssetsForProduct(IGrouping<string, AssetMapping> assetEntries, CatalogEntryDto productDto)
        {
            return assetEntries.SelectMany(CreateAssetsForGroups);
        }

        /// <summary>
        /// Creates the assets for all asset groups.
        /// </summary>
        /// <param name="assetEntry">The asset entry.</param>
        /// <returns>An enumeration of the asset for each asset group that was defined.</returns>
        private IEnumerable<AssetWrapper> CreateAssetsForGroups(AssetMapping assetEntry)
        {
            return ValidAssetGroups(assetEntry.AssetName).Select(g => CreateAssetWrapper(assetEntry, g));
        }

        /// <summary>
        /// Creates the asset wrapper.
        /// </summary>
        /// <param name="assetEntry">The entry defining the mapping of product to asset.</param>
        /// <param name="group">The asset group.</param>
        /// <returns>A wrapper for the asset.</returns>
        private AssetWrapper CreateAssetWrapper(AssetMapping assetEntry, string group)
        {
            var assetEntity = GetOrCreateElementEntity(AssetGroupFolder(assetEntry.ProductCode, group), assetEntry.AssetName, FullAssetPath(group, assetEntry.AssetName));
            return new AssetWrapper() { AssetKey = assetEntity.PrimaryKeyId.Value.ToString(), Group = group };
        }

        /// <summary>
        /// Clears cached information about asset folders.
        /// </summary>
        private void ResetFolders()
        {
            _assetGroupFolder.Clear();
            _productFolder = null;
        }

        /// <summary>
        /// Get an asset folder for the specified product and asset group.
        /// </summary>
        /// <param name="productCode">The product code.</param>
        /// <param name="group">The group.</param>
        /// <returns>An asset folder.</returns>
        private FolderEntity AssetGroupFolder(string productCode, string group)
        {
            var folder = _assetGroupFolder.SingleOrDefault(f => String.Equals(f.Name, group, StringComparison.OrdinalIgnoreCase));
            if (folder == null)
            {
                folder = GetOrCreateFolderEntity(ProductFolder(productCode), group);
                _assetGroupFolder.Add(folder);
            }
            return folder;
        }

        /// <summary>
        /// Gets an asset folder for the product.
        /// </summary>
        /// <param name="productCode">The product code.</param>
        /// <returns>An asset folder.</returns>
        private FolderEntity ProductFolder(string productCode)
        {
            if (_productFolder == null)
            {
                _productFolder = GetOrCreateFolderEntity(PackageFolder, productCode);
            }
            return _productFolder;
        }

        /// <summary>
        /// Gets the package folder.
        /// </summary>
        /// <remarks>
        /// The package is used to split up the folder structure since ECF has a (configurable) limit on the number of
        /// entries in a single folder.
        /// </remarks>
        private FolderEntity PackageFolder
        {
            get
            {
                var newPackageName = String.Format("Package{0:00}", (int)(_subFolderCount / MAX_ITEM_PER_PACKAGE));
                if (_packageName != newPackageName)
                {
                    _packageName = newPackageName;
                    _packageFolder = GetOrCreateFolderEntity(AssetFolder, _packageName);
                }
                return _packageFolder;
            }
        }
    }
}
