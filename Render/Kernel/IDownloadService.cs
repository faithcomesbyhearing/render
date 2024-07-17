using CommunityToolkit.Maui.Core.Primitives;

namespace Render.Kernel
{
    public interface IDownloadService
    {
        Task<string> ChooseFilePathAsync();

        Task DownloadAsync(byte[] fileData, string fileName);
        
        Task DownloadAsync(Stream stream, string fileName);
        
        Task<Folder> ChooseFileAsync();
    }
}