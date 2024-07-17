using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Audio;
using Render.Resources.Localization;
using Render.Services.AudioServices;

namespace Render.Components.SectionTitlePlayer
{
    public enum SectionTitlePlayerState
    {
        Empty,
        Quarter,
        Half,
        ThreeQuarters,
        Full
    }

    public class SectionTitlePlayerViewModel : ActionViewModelBase, ISectionTitlePlayerViewModel
    {
        public string SectionNumber { get; }
        public string PassageNumber { get; }

        public ReactiveCommand<Unit, Unit> ButtonClickCommand { get; }

        [Reactive]
        public SectionTitlePlayerState SectionTitlePlayerState { get; private set; }
        
        [Reactive]
        public bool HasAudio { get; private set;}
        
        [Reactive]
        public bool IsPlaying { get; private set; }

        private IAudioPlayerService _audioPlayerService;
        private ITempAudioService _tempAudioService;


        /// <summary>
        /// This constructor is for pages where we need a viewmodel to bind to the component, but the component has no
        /// function because we are not on a page to play a section title.
        /// </summary>
        /// <param name="viewModelContextProvider"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="passageNumber"></param>
        public SectionTitlePlayerViewModel(
            IViewModelContextProvider viewModelContextProvider,
            int sectionNumber,
            string passageNumber) : 
            base(ActionState.Optional, "SectionTitlePlayer", viewModelContextProvider)
        {
            SectionNumber = sectionNumber.ToString();
            PassageNumber = passageNumber;
        }

        public SectionTitlePlayerViewModel(
            Audio audio,
            IViewModelContextProvider viewModelContextProvider,
            int sectionNumber,
            string passageNumber) :
            this(viewModelContextProvider, sectionNumber, passageNumber)
        {
            _audioPlayerService = viewModelContextProvider.GetAudioPlayerService(Pause);

            Task.Run(() =>
            {
                _tempAudioService = viewModelContextProvider.GetTempAudioService(audio);

                _audioPlayerService.Load(_tempAudioService.OpenAudioStream());
            });

            HasAudio = true;
            ButtonClickCommand = ReactiveCommand.Create(ButtonClick);

            Disposables.Add(this
                .WhenAnyValue(player => player._audioPlayerService.CurrentPosition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(SetSectionTitleCircleFillAmount));
            
            Disposables.Add(this
                .WhenAnyValue(player => player._audioPlayerService.AudioPlayerState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    IsPlaying = state == AudioPlayerState.Playing;
                }));

            _audioPlayerService.OnPlayerEnd += AudioPlayerServiceOnPlayerEnd;
        }

        public void ButtonClick()
        {
            if (_audioPlayerService.AudioPlayerState == AudioPlayerState.Playing)
            {
                LogInfo("Stop Section Title Player", new Dictionary<string, string>
                {
                    {"Section Number", SectionNumber}
                });
                _audioPlayerService.Stop();
            }
            else
            {
                LogInfo("Start Section Title Player", new Dictionary<string, string>
                {
                    {"Section Number", SectionNumber}
                });
                _audioPlayerService.Play();
            }
        }

        private void AudioPlayerServiceOnPlayerEnd()
        {
            //Delay setting the player back to the beginning so the circle has a chance to finish filling and display
            //for half a second before resetting to being empty
            Thread.Sleep(500);

            _audioPlayerService.Seek(0);
        }

        public void SetSectionTitleCircleFillAmount(Double currentDuration)
        {
            var currentPercentage = currentDuration / _audioPlayerService.Duration;

            if (Math.Abs(currentPercentage - 1) < 0.01)
            {
                SectionTitlePlayerState = SectionTitlePlayerState.Full;
            }
            else if (currentPercentage >= 0.75)
            {
                SectionTitlePlayerState = SectionTitlePlayerState.ThreeQuarters;
            }
            else if (currentPercentage >= 0.5)
            {
                SectionTitlePlayerState = SectionTitlePlayerState.Half;
            }
            else if (currentPercentage >= 0.25)
            {
                SectionTitlePlayerState = SectionTitlePlayerState.Quarter;
            }
            else
            {
                SectionTitlePlayerState = SectionTitlePlayerState.Empty;
            }
        }

        public void Pause()
        {
            if(_audioPlayerService == null)
            {
                return;
            }

            if (_audioPlayerService.AudioPlayerState == AudioPlayerState.Playing)
            {
                _audioPlayerService.Stop();
            }
        }
        
        public override void Dispose()
        {
            Pause();

            if (_audioPlayerService != null)
            {
                _audioPlayerService.OnPlayerEnd -= AudioPlayerServiceOnPlayerEnd;
                _audioPlayerService.Dispose();
                _audioPlayerService = null;
            }
            _tempAudioService = null;
            ButtonClickCommand?.Dispose();

            base.Dispose();
        }
    }
}