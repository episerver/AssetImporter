using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Assets;
using Mediachase.Commerce.Assets.Import;

namespace EPiServer.Business.Commerce.Tools.ImportAsset
{
    /// <summary>
    /// An import implementation that will import assets according to the the structure of 'asset folder name' / 'asset group' / 'asset file'.
    /// </summary>
    /// <remarks>
    /// This is the most efficient way if many product re-use the same image, but may cause maintenance problems if assets are managed from within ECF
    /// due to the fact that one image is used in multiple places.
    /// </remarks>
    public class ImportByAsset : ImportBase
    {
        private List<FolderEntity> _assetGroupFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportByAsset"/> class.
        /// </summary>
        /// <param name="assetPath">The local file path where the files to import are present.</param>
        /// <param name="assetFolderName">Name of the asset folder that will be used/created in ECF.</param>
        public ImportByAsset(string assetPath, string assetFolderName)
            : base(assetPath, assetFolderName)
        {
            _assetGroupFolder = new List<FolderEntity>();
        }

        /// <summary>
        /// The main entry point for import.
        /// </summary>
        /// <remarks>
        /// Imports each asset file once (according to the information in the mapping file) and associates it with one or more products.
        /// </remarks>
        public override void DoImport(IEnumerable<AssetMapping> entries)
        {
            foreach (var productEntries in entries.GroupBy(e => e.AssetName))
            {
                var assets = CreateAssetsForGroups(productEntries.First().AssetName);

                foreach (var productEntry in productEntries)
                {
                    AssociateAssetsWithProductCode(assets, productEntry.ProductCode);
                }
            }
        }

        /// <summary>
        /// Imports all assets from available asset groups.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <returns>An enumeration of AssetWrapper.</returns>
        /// <remarks>
        /// Note that if the asset does not exist in all asset groups, then we will only import the assets that actually exists.
        /// </remarks>
        private IEnumerable<AssetWrapper> CreateAssetsForGroups(string assetName)
        {
            return ValidAssetGroups(assetName).Select(g => CreateAssetWrapper(assetName, g));
        }

        /// <summary>
        /// Associates the imported assets with a product/variation.
        /// </summary>
        /// <param name="assets">The assets.</param>
        /// <param name="productCode">The product code.</param>
        private void AssociateAssetsWithProductCode(IEnumerable<AssetWrapper> assets, string productCode)
        {
            using (var productDto = GetProductDto(productCode))
            {
                if (productDto == null)
                {
                    return;
                }
                AssociateAssetsWithProduct(assets, productDto);
            }
        }

        /// <summary>
        /// Imports the asset to the correct asset folder.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <param name="group">The asset group.</param>
        /// <returns>An AssetWrapper that can be used for associating the asset with a product/variation.</returns>
        private AssetWrapper CreateAssetWrapper(string assetName, string group)
        {
            // Get or create the correct folder for the asset group
            // Create the asset entity (if it does not exist), in that folder
            var asset = GetOrCreateElementEntity(AssetGroupFolder(group), assetName, FullAssetPath(group, assetName));

            // Pull out the asset key (since that is all we need for associations) and the group.
            return new AssetWrapper() { AssetKey = asset.PrimaryKeyId.Value.ToString(), Group = group };
        }

        private FolderEntity AssetGroupFolder(string group)
        {
            // Check if the folder has already been used, and therefore we already have the folder object. Note that this will do a linear
            // scan thru the _assetGroupFolder, but we expect there to be a very limited set of asset groups (about 2 or 3) and should
            // therefore not cause a performance issue)
            var folder = _assetGroupFolder.SingleOrDefault(f => String.Equals(f.Name, group, StringComparison.OrdinalIgnoreCase));
            if (folder == null)
            {
                // Get the folder if it already exists in ECF, or create it.
                folder = GetOrCreateFolderEntity(AssetFolder, group);
                _assetGroupFolder.Add(folder);
            }
            return folder;
        }
    }
}
