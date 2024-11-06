using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class DownloadService : IDownloadService
    {
        private Folder StorageFolder { get; set; }

        public async Task<Folder> ChooseFolderAsync()
        {
            var folderPickerResult = await FolderPicker.Default.PickAsync(CancellationToken.None);

            StorageFolder = folderPickerResult.Folder;
            return StorageFolder;
        }
    }
}