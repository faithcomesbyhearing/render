using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using Render.Kernel;

namespace Render.Platforms.Kernel
{
    public class DownloadService : IDownloadService
    {
        private Folder StorageFolder { get; set; }

        public async Task<string> ChooseFilePathAsync()
        {
            var folderPickerResult = await FolderPicker.Default.PickAsync(CancellationToken.None);
            StorageFolder = folderPickerResult.Folder;
            return StorageFolder?.Path;
        }
        
        public async Task<Folder> ChooseFileAsync()
        {
            var folderPickerResult = await FolderPicker.Default.PickAsync(CancellationToken.None);
            
            StorageFolder = folderPickerResult.Folder;
            return StorageFolder;
        }
        
        public async Task DownloadAsync(byte[] fileData, string fileName)
        {
            if (StorageFolder == null)
            {
                return;
            }

            using (FileStream filestream = new FileStream(Path.Combine(StorageFolder.Path, fileName), FileMode.Create))
            {
                await filestream.WriteAsync(fileData, 0, fileData.Length);
            }
        }
        
        public async Task DownloadAsync(Stream stream, string fileName)
        {
            if (StorageFolder == null)
            {
                return;
            }

            await using var filestream = new FileStream(Path.Combine(StorageFolder.Path, fileName), FileMode.Create);
            await stream.CopyToAsync(filestream);
        }
    }
}