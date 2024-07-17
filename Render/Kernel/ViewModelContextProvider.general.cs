using Render.Interfaces.EssentialsWrappers;
using Render.Services;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Splat;

namespace Render.Kernel
{
    public partial class ViewModelContextProvider : IViewModelContextProvider
    {
        public Func<int, IAudioRecorder> GetAudioRecorderFactory()
        {
            return (int preferredSmapleRate) => Locator.Current
                .GetService<IAudioRecorderFactory>()
                .Create(GetAppDirectory().TempAudio, preferredSmapleRate);
        }

        private IEssentialsWrapper GetEssentialsWrapper()
        {
            return new EssentialsWrapper(Locator.Current.GetService<ICustomMicrophonePermission>());
        }
    }
}