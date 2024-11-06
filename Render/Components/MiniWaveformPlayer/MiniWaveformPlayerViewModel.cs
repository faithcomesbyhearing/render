using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioServices;

namespace Render.Components.MiniWaveformPlayer
{
    public class MiniWaveformPlayerViewModel : BarPlayerViewModel, IMiniWaveformPlayerViewModel
    {
        public MiniWaveformPlayerViewModel(
            Audio audio,
            IViewModelContextProvider viewModelContextProvider,
            ActionState initialState,
            string audioTitle,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null)
            : this(
                  new AudioPlayback(audio),
                  viewModelContextProvider,
                  initialState,
                  audioTitle,
                  timeMarkers,
                  passageMarkers,
                  secondaryButtonIcon,
                  secondaryButtonClickCommand,
                  showSecondaryButton,
                  glyph)
        {
        }

        public MiniWaveformPlayerViewModel(
            AudioPlayback audioPlayback,
            IViewModelContextProvider viewModelContextProvider,
            ActionState initialState,
            string audioTitle,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            bool showSecondaryButton = true,
            string glyph = null)
            : base(
                  audioPlayback,
                  viewModelContextProvider,
                  initialState,
                  audioTitle,
                  playerPositionInList: 0,
                  timeMarkers,
                  passageMarkers,
                  secondaryButtonIcon,
                  secondaryButtonClickCommand,
                  showSecondaryButton: showSecondaryButton,
                  glyph: glyph)
        {
            AudioSamples = audioPlayback.Samples ?? viewModelContextProvider.GetWaveFormService()
                                                                            .GetEmptyMiniWaveformBars();

            Disposables.Add(this.WhenAnyValue(s => s.AudioPlayerService.AudioPlayerState)
                .Where(audioPlayerState => audioPlayerState == AudioPlayerState.Loaded)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    if (audioPlayback.Samples is not null)
                    {
                        return;
                    }

                    AudioSamples = await viewModelContextProvider.GetWaveFormService()
                                                                 .GetMiniWaveformBarsAsync(TempAudioService.OpenAudioStream());
                }));
        }

        public override void Dispose()
        {
            AudioSamples = null;

            base.Dispose();
        }
    }
}