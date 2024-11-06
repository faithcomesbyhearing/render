using Render.Kernel.WrappersAndExtensions;
using Render.Repositories.Extensions;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Kernel;

public class UsbSyncFolderStorageService : IUsbSyncFolderStorageService
{
    private string UsbSyncFolderPath { get; set; }
    private string UsbSyncFolderName { get; set; }
    
    private readonly IModalService _modalService;
    
    public UsbSyncFolderStorageService(IModalService modalService)
    {
        _modalService = modalService;
    }
    
    public void SetUsbSyncFolder(string path, string folderName)
    {
        UsbSyncFolderPath = path;
        UsbSyncFolderName = folderName;
    }
        
    public async Task<string> GetUsbSyncFolder(Guid currentProjectId)
    {
        if (UsbSyncFolderPath.IsNullOrEmpty()) return null;

        if (CheckIfTheSameProjectIsSynchronized(currentProjectId))
        {
            return UsbSyncFolderPath;
        }
        
        await _modalService.ShowInfoModal(Icon.PopUpWarning, AppResources.SyncPathIsNotAvailable,
            AppResources.ChooseAnotherStorage)!;
        SetUsbSyncFolder(default, default);
        return null;
    }

    private bool CheckIfTheSameProjectIsSynchronized(Guid currentProjectId)
    {
        var projectNameArray = UsbSyncFolderName.Split('_');

        if (!Guid.TryParse(projectNameArray[1], out var parsedGuid)) return false;
        
        return parsedGuid == currentProjectId;
    }
}