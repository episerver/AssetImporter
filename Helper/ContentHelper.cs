using System;
using System.IO;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace EPiServer.Business.Commerce.Tools.ImportAsset.Helper
{
    public static class ContentHelper
    {
        private static IContentRepository _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        private static IContentTypeRepository _contenttypeRepository = ServiceLocator.Current.GetInstance<IContentTypeRepository>();
        private static IBlobFactory _blobFactory = ServiceLocator.Current.GetInstance<IBlobFactory>();
        private static ContentMediaResolver _contentMediaResolver = ServiceLocation.ServiceLocator.Current.GetInstance<ContentMediaResolver>();

        public static Guid CreateFileContent(FileInfo assetFile, string parentFolder)
        {
            var catalogRootFolder = GetRootFolderContent(parentFolder);
            var contentType = _contenttypeRepository.Load(_contentMediaResolver.GetFirstMatching(assetFile.Extension));
            var file = _contentRepository.GetDefault<MediaData>(catalogRootFolder.ContentLink, contentType.ID);
            file.Name = assetFile.Name;
            var blob = _blobFactory.CreateBlob(file.BinaryDataContainer, assetFile.Extension);
            FileStream fs = assetFile.OpenRead();
            blob.Write(fs);
            file.BinaryData = blob;
            var content = _contentRepository.Save(file, DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
            fs.Close();
            return file.ContentGuid;
        }

        private static ContentFolder GetRootFolderContent(string parentFolder)
        {
            var rootFolder = _contentRepository.GetChildren<ContentFolder>(SiteDefinition.Current.GlobalAssetsRoot).Where(f => string.Compare(f.Name, parentFolder, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault() as ContentFolder;
            if (rootFolder != null)
            {
                return rootFolder;
            }
            else
            {
                var globalMediaRootFolder = _contentRepository.Get<ContentFolder>(SiteDefinition.Current.GlobalAssetsRoot);
                var catalogRootFolder = _contentRepository.GetDefault<ContentFolder>(globalMediaRootFolder.ContentLink);
                catalogRootFolder.Name = parentFolder;

                var theFolderRef = _contentRepository.Save(catalogRootFolder, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
                // _contentRepository.Save(parentFolder, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);

                return _contentRepository.Get<ContentFolder>(theFolderRef);
            }
        }
    }
}
