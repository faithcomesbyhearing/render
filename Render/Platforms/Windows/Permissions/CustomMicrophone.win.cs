using Render.Services;
using Windows.Media.Capture;

namespace Render.Platforms.Permissions
{
    public class CustomMicrophone : Microsoft.Maui.ApplicationModel.Permissions.Microphone, ICustomMicrophonePermission
    {
        public override async Task<PermissionStatus> CheckStatusAsync()
        {
            var mediaCapture = new MediaCapture();

            try
            {
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    MediaCategory = MediaCategory.Media,
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                });
            }
            catch (UnauthorizedAccessException)
            {
                return PermissionStatus.Denied;
            }
            catch
            {
                throw;
            }
            finally
            {
                mediaCapture.Dispose();
            }

            return PermissionStatus.Granted;
        }

        public override Task<PermissionStatus> RequestAsync()
        {
            return Task.FromResult(PermissionStatus.Denied);
        }
    }
}