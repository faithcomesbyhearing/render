using Render.Interfaces.EssentialsWrappers;
using Render.Mocks.AudioRecorder;
using Render.Mocks.EssentialWrappers;
using Render.Services;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Splat;

namespace Render.Kernel
{
    public partial class ViewModelContextProvider : IViewModelContextProvider
    {
        public Func<int, IAudioRecorder> GetAudioRecorderFactory()
        {
            return (int preferredSmapleRate) => new AudioRecorderMock(
                tempAudioDirectory: GetAppDirectory().TempAudio, 
                preferredSampleRate: preferredSmapleRate);
        }

        private IEssentialsWrapper GetEssentialsWrapper()
        {
            return new EssentialsWrapperMock(new EssentialsWrapper(Locator.Current.GetService<ICustomMicrophonePermission>()));
        }
    }
}