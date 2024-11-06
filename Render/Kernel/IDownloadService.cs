using CommunityToolkit.Maui.Core.Primitives;

namespace Render.Kernel
{
    public interface IDownloadService
    {
        Task<Folder> ChooseFolderAsync();
    }
}