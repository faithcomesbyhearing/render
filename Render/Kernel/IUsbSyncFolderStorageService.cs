namespace Render.Kernel;

public interface IUsbSyncFolderStorageService
{
    void SetUsbSyncFolder(string path, string folderName);
    Task<string> GetUsbSyncFolder(Guid currentProjectId);
}