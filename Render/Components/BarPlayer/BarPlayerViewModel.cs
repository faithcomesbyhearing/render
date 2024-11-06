using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioServices;

namespace Render.Components.BarPlayer
{
    public class BarPlayerViewModel : ActionViewModelBase, IBarPlayerViewModel
    {
        private bool _resumeAfterSeek;
        private CancellationTokenSource _tempAudioServiceCancellation;
        protected ITempAudioService TempAudioService { get; private set; }

        [Reactive] public string Glyph { get; set; }

        [Reactive] public string AudioTitle { get; set; }

        [Reactive] public double CurrentPosition { get; set; }

        [Reactive] public double Duration { get; private set; }

        [Reactive] public bool ShowPlayButton { get; private set; }

        [Reactive] public bool ShowPauseButton { get; private set; }

        [Reactive] public AudioPlayerState AudioPlayerState { get; set; }

        [Reactive] public bool Loading { get; set; }

        [Reactive] public bool ShowSecondaryButton { get; private set; }

        [Reactive] public ImageSource SecondaryButtonIcon { get; private set; }

        [Reactive] public Color SecondaryButtonBackgroundColor { get; set; }

		[Reactive] public Color GlyphColor { get; private set; }

		[Reactive] public float[] AudioSamples { get; set; }

        [Reactive] public BarPlayerPassagePainter PassagePainter { get; set; }

        public int PlayerPositionInList { get; }
        public bool PaintPassageMarkers { get; set; }
        public IAudioPlayerService AudioPlayerService { get; private set; }

        public ReactiveCommand<Unit, IRoutableViewModel> SecondaryButtonClickCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> PlayAudioCommand { get; protected set; }
        public ReactiveCommand<Unit, Unit> PauseAudioCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> SeekCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> PauseOnSeekCommand { get; private set; }
        protected string WavAudioPath { get; private set; }

        public BarPlayerViewModel(
            Media media,
            IViewModelContextProvider viewModelContextProvider,
            ActionState initialState,
            string audioTitle,
            TimeMarkers timeMarkers = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null,
            bool showSecondaryButton = true) :
            this(viewModelContextProvider, media.AudioId, initialState)
        {
            Glyph = glyph;

            if (secondaryButtonIcon != null && secondaryButtonClickCommand != null)
            {
                ShowSecondaryButton = showSecondaryButton;
                SecondaryButtonIcon = secondaryButtonIcon;
                SecondaryButtonClickCommand = secondaryButtonClickCommand;
            }

            Setup(audioTitle, viewModelContextProvider, canPlayAudio);

            if (media.HasAudio)
            {
                LoadAudio(new AudioPlayback(media.Audio), timeMarkers);
            }
        }

        public BarPlayerViewModel(
            Audio audio,
            IViewModelContextProvider viewModelContextProvider,
            ActionState initialState,
            string audioTitle,
            int playerPositionInList,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageDivisions = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null,
            bool showSecondaryButton = true)
            : this(new AudioPlayback(audio),
                viewModelContextProvider, initialState, audioTitle, playerPositionInList, timeMarkers, passageDivisions,
                secondaryButtonIcon, secondaryButtonClickCommand, canPlayAudio, glyph, showSecondaryButton)
        {
        }

        public BarPlayerViewModel(AudioPlayback audioPlayback,
            IViewModelContextProvider viewModelContextProvider,
            ActionState initialState,
            string audioTitle,
            int playerPositionInList,
            TimeMarkers timeMarkers = null,
            List<TimeMarkers> passageDivisions = null,
            ImageSource secondaryButtonIcon = null,
            ReactiveCommand<Unit, IRoutableViewModel> secondaryButtonClickCommand = null,
            IObservable<bool> canPlayAudio = null,
            string glyph = null,
            bool showSecondaryButton = true) :
            this(viewModelContextProvider, audioPlayback.AudioId, initialState)
        {
            Glyph = glyph;
            if (secondaryButtonIcon != null && secondaryButtonClickCommand != null)
            {
                ShowSecondaryButton = showSecondaryButton;
                SecondaryButtonIcon = secondaryButtonIcon;
                SecondaryButtonClickCommand = secondaryButtonClickCommand;
            }

            Setup(audioTitle, viewModelContextProvider, canPlayAudio);
            PlayerPositionInList = playerPositionInList;
            if (passageDivisions != null)
            {
                PassagePainter = new BarPlayerPassagePainter(passageDivisions);
                PaintPassageMarkers = true;
            }

            LoadAudio(audioPlayback, timeMarkers);
        }

        private BarPlayerViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Guid mediaId,
            ActionState initialState)
            : base(initialState, mediaId, "BarPlayer", viewModelContextProvider) { }

        public void SetPassages(List<TimeMarkers> passageMarkers)
        {
            PassagePainter = new BarPlayerPassagePainter(passageMarkers);
            PassagePainter.UpdateDuration(Duration);
            PaintPassageMarkers = true;
        }

        private async void LoadAudio(AudioPlayback audioPlayback, TimeMarkers timeMarkers = null)
        {
            try
            {
                if (audioPlayback.HasAudioData is false)
                {
                    return;
                }

                _tempAudioServiceCancellation?.Cancel();
                _tempAudioServiceCancellation = new CancellationTokenSource();
                TempAudioService = ViewModelContextProvider.GetTempAudioService(audioPlayback);

                var audioStream = await TempAudioService.OpenAudioStreamAsync(_tempAudioServiceCancellation.Token);

                if (AudioPlayerService != null && audioStream != null && _tempAudioServiceCancellation.IsCancellationRequested is false)
                {
                    await AudioPlayerService.LoadAsync(audioStream, timeMarkers);
                }
            }
            catch (Exception e)
            {
                var properties = new Dictionary<string, string>
                {
                    { "AudioId", audioPlayback.AudioId.ToString() },
                    { "Audio Title", AudioTitle }
                };
                LogError(e, properties);
            }
        }

        protected virtual void Setup(string audioTitle, IViewModelContextProvider viewModelContextProvider, IObservable<bool> canPlayAudio = null)
        {
            AudioTitle = audioTitle;
            AudioPlayerService = viewModelContextProvider.GetAudioPlayerService(Pause);
            PlayAudioCommand = canPlayAudio != null
                ? ReactiveCommand.Create(AudioPlayerService.Play, canPlayAudio)
                : ReactiveCommand.Create(AudioPlayerService.Play);

            PauseAudioCommand = ReactiveCommand.Create(Pause);
            PauseOnSeekCommand = ReactiveCommand.Create(PauseOnSeek);
            SeekCommand = ReactiveCommand.Create(OnSeek);

            Disposables.Add(this
                .WhenAnyValue(x => x.AudioPlayerService.Duration)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(d =>
                {
                    Duration = d > 0.0 ? d : 1.0;
                    if (PaintPassageMarkers)
                    {
                        PassagePainter.UpdateDuration(Duration);
                    }
                }));

            Disposables.Add(this
                .WhenAnyValue(s => s.AudioPlayerService.AudioPlayerState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v =>
                {
                    AudioPlayerState = v;
                    switch (v)
                    {
                        case AudioPlayerState.Playing:
                            ShowPlayButton = false;
                            ShowPauseButton = true;
                            Loading = false;
                            break;
                        case AudioPlayerState.Loaded:
                        case AudioPlayerState.Paused:
                            ShowPauseButton = false;
                            ShowPlayButton = true;
                            Loading = false;
                            break;
                        case AudioPlayerState.Initial:
                            ShowPauseButton = false;
                            ShowPlayButton = false;
                            Loading = true;
                            break;
                    }
                }));

            Disposables.Add(this
                .WhenAnyValue(s => s.AudioPlayerService.CurrentPosition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(d =>
                {
                    if (!_resumeAfterSeek)
                    {
                        CurrentPosition = d;
                    }
                }));

            AudioPlayerService.OnPlayerEnd += AudioPlayerServiceOnOnPlayerEnd;
        }

        public void Seek(double position)
        {
            PauseOnSeek();
            CurrentPosition = position;
            OnSeek();
        }

        private void OnSeek()
        {
            AudioPlayerService.Seek(CurrentPosition);
            if (_resumeAfterSeek)
            {
                AudioPlayerService.Play();
                _resumeAfterSeek = false;
            }
        }

        public virtual void Pause()
        {
            if (AudioPlayerService != null && AudioPlayerService.AudioPlayerState == AudioPlayerState.Playing)
            {
                AudioPlayerService.Pause();
                SetState(ActionState.Optional);
            }
        }

        protected virtual void AudioPlayerServiceOnOnPlayerEnd()
        {
            AudioPlayerService.Seek(0);
            SetState(ActionState.Optional);

            LogInfo("Bar Player Ended", new Dictionary<string, string>
            {
                { "Bar Player Title", AudioTitle },
                { "Bar Player Position", PlayerPositionInList.ToString() }
            });
        }

        private void PauseOnSeek()
        {
            if (AudioPlayerService.AudioPlayerState == AudioPlayerState.Playing)
            {
                _resumeAfterSeek = true;
                AudioPlayerService.Pause();
            }
        }

        public void SetSecondaryButtonBackgroundColor(Color color)
        {
            SecondaryButtonBackgroundColor = color;
        }

		public void SetGlyphColor(Color color)
		{
			GlyphColor = color;
		}

		public override void Dispose()
        {
            Pause();

            SecondaryButtonClickCommand?.Dispose();
            PlayAudioCommand?.Dispose();
            PauseAudioCommand?.Dispose();
            SeekCommand?.Dispose();
            PauseOnSeekCommand?.Dispose();

            SecondaryButtonClickCommand = null;
            PlayAudioCommand = null;
            PauseAudioCommand = null;
            SeekCommand = null;
            PauseOnSeekCommand = null;

            SecondaryButtonIcon = null;
            PassagePainter = null;

            if (AudioPlayerService != null)
            {
                AudioPlayerService.OnPlayerEnd -= AudioPlayerServiceOnOnPlayerEnd;
                AudioPlayerService.Dispose();
                AudioPlayerService = null;
            }
            
            _tempAudioServiceCancellation?.Cancel();
            _tempAudioServiceCancellation?.Dispose();
            _tempAudioServiceCancellation = null;

            TempAudioService?.Dispose();
            TempAudioService = null;

            base.Dispose();
        }
    }
}