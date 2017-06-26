using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPiServer.Business.Commerce.Tools.ImportAsset.Helper;
using EPiServer.Business.Commerce.Tools.ImportAsset.Util;
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
    public class ImportByCmsContent : ImportBase
    {
        private string _folderPath;
        private string _importFolder;
        private string _sitePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportByCmsContent"/> class.
        /// </summary>
        /// <param name="assetPath">The local file path where the files to import are present.</param>
        /// <param name="assetFolderName">Name of the asset folder that will be used/created in ECF.</param>
        /// <param name="sitePath">The site path.</param>
        public ImportByCmsContent(string assetPath, string assetFolderName, string sitePath)
            : base(assetPath, assetFolderName)
        {
            _folderPath = assetPath;
            _importFolder = assetFolderName;
            _sitePath = sitePath;
        }

        /// <summary>
        /// The main entry point for import.
        /// </summary>
        /// <remarks>
        /// Imports each asset file once (according to the information in the mapping file) and associates it with one or more products.
        /// </remarks>
        public override void DoImport(IEnumerable<AssetMapping> entries)
        {
            RuntimeProxyWrapper _runtimeProxyWrapper = new RuntimeProxyWrapper(_sitePath);
            _runtimeProxyWrapper.GetSiteProxy().Initialize(_sitePath, "/");
            _runtimeProxyWrapper.GetSiteProxy().ImportAsset(entries, _folderPath, _importFolder);
        }

    }
}