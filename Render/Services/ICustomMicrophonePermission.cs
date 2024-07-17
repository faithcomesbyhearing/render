namespace Render.Services;

public interface ICustomMicrophonePermission
{
    Task<PermissionStatus> CheckStatusAsync();

    Task<PermissionStatus> RequestAsync();
}
