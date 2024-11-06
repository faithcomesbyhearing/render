using Render.Interfaces.EssentialsWrappers;
using Render.Services;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.AudioServices;
using Splat;

namespace Render.Kernel
{
    public partial class ViewModelContextProvider : IViewModelContextProvider
    {
        public Func<int, IAudioRecorder> GetAudioRecorderFactory()
        {
            return (int preferredSmapleRate) => Locator.Current
                .GetService<IAudioRecorderFactory>()
                .Create(
                    tempDirectory: GetAppDirectory().TempAudio,
                    sampleRate: preferredSmapleRate, 
                    deviceMonitor: Locator.Current.GetService<IAudioDeviceMonitor>());
        }

        private IEssentialsWrapper GetEssentialsWrapper()
        {
            return new EssentialsWrapper(Locator.Current.GetService<ICustomMicrophonePermission>());
        }
    }
}