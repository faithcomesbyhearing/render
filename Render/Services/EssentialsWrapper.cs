using Render.Interfaces.EssentialsWrappers;

namespace Render.Services
{
    public class EssentialsWrapper : IEssentialsWrapper
    {
        private readonly ICustomMicrophonePermission _microphonePermission;

        public EssentialsWrapper(ICustomMicrophonePermission microphonePermission)
        {
            _microphonePermission = microphonePermission;
        }

        public async Task<bool> CheckForAudioPermissions()
        {
            var status =  await _microphonePermission.CheckStatusAsync();
            return status == PermissionStatus.Granted;
        }

        public async Task<bool> AskForAudioPermissions()
        {
            var result = await _microphonePermission.RequestAsync();
            return result == PermissionStatus.Granted;
        }

        public async Task OpenBrowserAsync(string uri)
        {
            await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }

        public async Task<bool> AskForFileAccessPermissions()
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.StorageRead>();
            }
            var permission2 = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (permission2 != PermissionStatus.Granted)
            {
                permission2 = await Permissions.RequestAsync<Permissions.StorageWrite>();
            }
            return permission == PermissionStatus.Granted && permission2 == PermissionStatus.Granted;
        }

        public void InvokeOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }
}