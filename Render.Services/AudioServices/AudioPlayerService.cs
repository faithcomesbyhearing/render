using System.Timers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces.AudioServices;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioPlugins.AudioPlayer;
using Timer = System.Timers.Timer;

namespace Render.Services.AudioServices
{
    public enum AudioPlayerState
    {
        Initial,
        Loaded,
        Playing,
        Paused
    }

    public class AudioPlayerService : ReactiveObject, IAudioPlayerService
    {
        [Reactive] public AudioPlayerState AudioPlayerState { get; private set; }

        /// <summary>
        /// The duration relative to the start time offset if only playing partial audio
        /// </summary>
        [Reactive]
        public double Duration { get; private set; }

        /// <summary>
        /// The current position relative to the start time offset if only playing partial audio
        /// </summary>
        [Reactive]
        public double CurrentPosition { get; private set; }

        public IAudioPlayer SimpleAudioPlayer { get; private set; }
        
        /// <summary>
        /// The absolute position of the time at which the audio should begin playing
        /// </summary>
        private double _startTime;

        /// <summary>
        /// The absolute position of the time at which the audio should stop being played
        /// </summary>
        private double _endTime;

        private bool _isPlayingPartialAudio;

        public event Action OnPlayerEnd;
        private readonly Timer _playbackTimerForPosition = new Timer(100);
        private readonly IAudioActivityService _audioActivityService;
        private readonly Action _stopAudioActivityCommand;

        public void Unload()
        {
            AudioPlayerState = AudioPlayerState.Initial;
            CurrentPosition = 0;
        }

        public AudioPlayerService(IAudioPlayer simpleAudioPlayer)
        {
            SimpleAudioPlayer = simpleAudioPlayer;
            SimpleAudioPlayer.PlaybackEnded += OnPlayerEnded;

            SetState(AudioPlayerState.Initial);

            _playbackTimerForPosition.Elapsed += WatchPositionAsync;
            _playbackTimerForPosition.AutoReset = true;

            SimpleAudioPlayer
                .WhenAnyValue(x => x.Duration)
                .Subscribe(d => { Duration = _isPlayingPartialAudio ? Duration : d; });
        }

        public AudioPlayerService(
            IAudioPlayer simpleAudioPlayer, 
            IAudioActivityService audioActivityService, 
            Action stopAudioActivityCommand) :
            this(simpleAudioPlayer)
        {
            _audioActivityService = audioActivityService;
            _stopAudioActivityCommand = stopAudioActivityCommand;
        }

        public void Load(Audio audio, TimeMarkers timeMarkers = default, bool dataIsWav = false)
            => Load(new MemoryStream(audio.Data), timeMarkers, dataIsWav);

        public void Load(Stream audioStream, TimeMarkers timeMarkers = default, bool dataIsWav = false)
        {
            FinishLoad(SimpleAudioPlayer.Load(audioStream, dataIsWav), timeMarkers);
            CurrentPosition = 0;
        }
        public async Task LoadAsync(Stream audioStream, TimeMarkers timeMarkers)
        {
            bool loaded = await SimpleAudioPlayer.LoadAsync(audioStream);
            FinishLoad(loaded, timeMarkers);
            CurrentPosition = 0;
        }

        /// <summary>
        /// Expects the start and end times to be in int milliseconds which it will convert to double seconds
        /// </summary>
        private void FinishLoad(bool loaded, TimeMarkers timeMarkers)
        {
            if (loaded)
            {
                if (timeMarkers != null && timeMarkers.EndMarkerTime != 0
                                        && timeMarkers.EndMarkerTime > timeMarkers.StartMarkerTime
                                        && Math.Round((double)timeMarkers.EndMarkerTime / 1000, 1) <= Math.Round(SimpleAudioPlayer.Duration, 1))
                {
                    _isPlayingPartialAudio = true;
                    _startTime = ((double)timeMarkers.StartMarkerTime) / 1000;
                    _endTime = ((double)timeMarkers.EndMarkerTime) / 1000;
                    Duration = _endTime - _startTime;
                    SimpleAudioPlayer.Seek(_startTime);
                }
                else
                {
                    _isPlayingPartialAudio = false;
                    _startTime = 0;
                    _endTime = SimpleAudioPlayer.Duration;
                    Duration = SimpleAudioPlayer.Duration;
                }

                SetState(AudioPlayerState.Loaded);
            }
            else
            {
                throw new Exception("Audio did not load");
            }
        }

        public void Play()
        {
            if (AudioPlayerState == AudioPlayerState.Initial || AudioPlayerState == AudioPlayerState.Playing)
            {
                return;
            }

            SimpleAudioPlayer.Play();
            _playbackTimerForPosition.Start();
            SetState(AudioPlayerState.Playing);

            _audioActivityService?.SetStopCommand(_stopAudioActivityCommand, false);
        }

        public void Pause()
        {
            if (AudioPlayerState == AudioPlayerState.Playing)
            {
                SimpleAudioPlayer?.Pause();
                _playbackTimerForPosition.Stop();
                SetPosition();
                SetState(AudioPlayerState.Paused);
            }
        }

        /// <summary>
        /// Expects time relative to any offset for partial audio, not absolute time
        /// </summary>
        public void Seek(double time)
        {
            SimpleAudioPlayer.Seek(time + _startTime);
            SetPosition();
            if (AudioPlayerState == AudioPlayerState.Loaded)
                SetState(AudioPlayerState.Paused);
        }

        public void Stop()
        {
            SimpleAudioPlayer?.Stop();
            _playbackTimerForPosition?.Stop();
            SetState(AudioPlayerState.Loaded);
            if (_isPlayingPartialAudio)
            {
                SimpleAudioPlayer?.Seek(_startTime);
                SetPosition();
            }
        }

        /// <summary>
        /// Public only for testing purposes
        /// </summary>
        // public void WatchPosition(double currentPosition)
        // {
        //     if (_isPlayingPartialAudio && currentPosition >= _endTime)
        //     {
        //         Stop();
        //         OnPlayerEnd?.Invoke();
        //     }
        // }
        
        /// <summary>
        /// Public only for testing purposes
        /// </summary>
        public void WatchPositionAsync(object sender, ElapsedEventArgs e)
        {
            SetPosition();
            if (_isPlayingPartialAudio && (SimpleAudioPlayer != null && SimpleAudioPlayer.CurrentPosition >= _endTime))
            {
                Stop();
                OnPlayerEnd?.Invoke();
            }
        }
        
        /// <summary>
        /// Sets the relative current position based on the offset of partial audio
        /// </summary>
        private void SetPosition()
        {
            if(SimpleAudioPlayer != null)
            { 
                CurrentPosition = SimpleAudioPlayer.CurrentPosition - _startTime;
            }
        }

        private void OnPlayerEnded(object sender, EventArgs e)
        {
            if(Duration == 0) return;
            SetState(AudioPlayerState.Loaded);
            OnPlayerEnd?.Invoke();
        }

        private void SetState(AudioPlayerState state)
        {
            AudioPlayerState = state;
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                OnPlayerEnd = null;

                _audioActivityService?.Dispose();

                _playbackTimerForPosition.Elapsed -= WatchPositionAsync;
                _playbackTimerForPosition.Stop();
                _playbackTimerForPosition.Dispose();

                if (SimpleAudioPlayer != null)
                {
                    SimpleAudioPlayer.PlaybackEnded -= OnPlayerEnded;
                    SimpleAudioPlayer.Stop();
                    SimpleAudioPlayer.Dispose();
                    SimpleAudioPlayer = null;
                }
            }

            _disposed = true;
        }
    }
}