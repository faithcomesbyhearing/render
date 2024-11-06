using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioServices;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Render.Platforms.Kernel.AudioPlayer;

public class AudioPlayer : IAudioPlayer
{
    private IAudioDeviceMonitor _deviceMonitor;

    private bool _isDisposed;

    private double? _startTime;
    private double? _endTime;

    /// <summary>
    /// Playback end event handler
    /// </summary>
    public event EventHandler PlaybackEnded;

    /// <summary>
    /// Playback failed event handler.
    /// TODO: improve to send specific error codes to client
    /// </summary>
    public event EventHandler PlaybackFailed;

    /// <summary>
    /// Media Source object
    /// </summary>
    private MediaSource _mediaSource;

    /// <summary>
    /// Preloaded position in seconds to navigate player on creation
    /// </summary>
    private double _preloadedPosition;

    /// <summary>
    /// Media player object
    /// </summary>
    private MediaPlayer _player;

    /// <summary>
    /// Media player interface
    /// </summary>
    private MediaPlayer Player
    {
        get => _player ??= GetPlayer();
    }

    private bool HasActiveDevice
    {
        get => _deviceMonitor.HasActiveAudioOutputDevice;
    }

    ///<Summary>
    /// Length of audio in seconds (considering StartTime & EndTime)
    ///</Summary>
    public double Duration
    {
        get => EndTime!.Value - StartTime!.Value;
    }

    ///<Summary>
    /// Total length of audio in seconds (ignoring StartTime & EndTime)
    ///</Summary>
    public double TotalDuration
    {
        get => _mediaSource?.Duration?.TotalSeconds ?? 0;
    }

    ///<Summary>
    /// Current position of audio playback in seconds (considering StartTime & EndTime)
    ///</Summary>
    public double CurrentPosition
    {
        get => _player is null ?
            _preloadedPosition :
            _player.PlaybackSession.Position.TotalMilliseconds / 1000 - StartTime!.Value;
    }

    ///<Summary>
    /// Current position of audio playback in seconds (ignoring StartTime & EndTime)
    ///</Summary>
    public double TotalCurrentPosition
    {
        get => _player is null ?
            _preloadedPosition :
            _player.PlaybackSession.Position.TotalMilliseconds / 1000;
    }

    ///<Summary>
    /// Playback volume (0 to 1)
    ///</Summary>
    public double Volume
    {
        get => _player?.Volume ?? 0;
        set => SetVolume(value, Balance);
    }

    ///<Summary>
    /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
    ///</Summary>
    public double Balance
    {
        get => _player?.AudioBalance ?? 0;
        set => SetVolume(Volume, value);
    }

    ///<Summary>
    /// Continuously repeats the currently playing sound
    ///</Summary>
    public bool Loop
    {
        get => _player?.IsLoopingEnabled ?? false;
        set
        {
            if (_player is null)
            {
                return;
            }

            _player.IsLoopingEnabled = value;
        }
    }

    ///<Summary>
    /// Indicates if the position of the loaded audio file can be updated
    ///</Summary>
    public bool CanSeek
    {
        get => _player?.PlaybackSession.CanSeek ?? false;
    }

    ///<Summary>
    /// Indicates if the player is playing
    ///</Summary>
    public bool IsPlaying
    {
        get => _player?.CurrentState is MediaPlayerState.Playing;
    }

    public double? StartTime
    {
        get => _startTime ?? 0;
        set
        {
            _startTime = value;
            OnStartTimeChanged(value);
        }
    }

    public double? EndTime
    {
        get => _endTime ?? _mediaSource?.Duration?.TotalSeconds ?? 0;
        set
        {
            _endTime = value;
            OnEndTimeChanged(value);
        }
    }

    public AudioPlayer(IAudioDeviceMonitor deviceMonitor)
    {
        _deviceMonitor = deviceMonitor;
    }

    ///<Summary>
    /// Load wave or opus audio file from a stream
    ///</Summary>
    public async Task<bool> LoadAsync(Stream audioStream, bool isWav = false)
    {
        DeleteMediaSource();

        _mediaSource = MediaSource.CreateFromStream(audioStream?.AsRandomAccessStream(), string.Empty);
        await _mediaSource.OpenAsync();

        return _mediaSource != null;
    }

    ///<Summary>
    /// Load wave or mp3 audio file from assets folder in the UWP project
    ///</Summary>
    public async Task<bool> LoadAsync(string fileName)
    {
        DeleteMediaSource();

        _mediaSource = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/" + fileName));
        await _mediaSource.OpenAsync();

        return _mediaSource != null;
    }

    ///<Summary>
    /// Load wave or opus audio file from a stream
    ///</Summary>
    public bool Load(Stream audioStream, bool isWav = false)
    {
        DeleteMediaSource();

        _mediaSource = MediaSource.CreateFromStream(audioStream?.AsRandomAccessStream(), string.Empty);
        Task.Run(async () => await _mediaSource.OpenAsync()).Wait();

        return _mediaSource != null;
    }

    ///<Summary>
    /// Load wave or mp3 audio file from assets folder in the UWP project
    ///</Summary>
    public bool Load(string fileName)
    {
        DeleteMediaSource();

        _mediaSource = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/" + fileName));
        Task.Run(async () => await _mediaSource.OpenAsync()).Wait();

        return _mediaSource != null;
    }

    public void Unload()
    {
        DeleteMediaSource();
    }

    private void DeletePlayer()
    {
        if (_player == null)
            return;

        _player.MediaEnded -= OnPlaybackEnded;
        _player.Source = null;
        _player.Dispose();
        _player = null;
    }

    private void OnPlaybackEnded(MediaPlayer sender, object args)
    {
        PlaybackEnded?.Invoke(sender, EventArgs.Empty);
    }

    ///<Summary>
    /// Begin playback or resume if paused
    ///</Summary>
    public void Play()
    {
        if (HasActiveDevice is false)
        {
            return;
        }

        Player.Play();
    }

    ///<Summary>
    /// Pause playback if playing (does not resume)
    ///</Summary>
    public void Pause()
    {
        Player.Pause();
    }

    ///<Summary>
    /// Stop playback and set the current position to the beginning
    ///</Summary>
    public void Stop()
    {
        if (HasActiveDevice is false)
        {
            return;
        }

        if (_player != null)
        {
            Pause();
            Seek(0);
        }
    }

    ///<Summary>
    /// Seek a position in seconds in the currently loaded sound file 
    ///</Summary>
    public void Seek(double position)
    {
        if (HasActiveDevice is false)
        {
            return;
        }

        if (_player == null)
        {
            _preloadedPosition = position;
        }
        else
        {
            position = Math.Min(position + StartTime!.Value, EndTime!.Value);
            Player.PlaybackSession.Position = TimeSpan.FromSeconds(position);
        }
    }

    private void SetVolume(double volume, double balance)
    {
        Player.Volume = Math.Min(1, Math.Max(0, volume));
        Player.AudioBalance = Math.Min(1, Math.Max(-1, balance));
    }

    private MediaPlayer GetPlayer()
    {
        if (_mediaSource is null)
        {
            throw new InvalidOperationException("MediaSource is not initialized");
        }

        var player = new MediaPlayer
        {
            AutoPlay = false,
            AudioBalance = 0,
            Source = _mediaSource
        };

        player.MediaEnded += OnPlaybackEnded;

        player.PlaybackSession.Position = TimeSpan.FromSeconds(_preloadedPosition + _startTime.GetValueOrDefault(0));
        player.PlaybackMediaMarkerReached += EndTimeReached;

        return player;
    }

    private void EndTimeReached(MediaPlayer sender, PlaybackMediaMarkerReachedEventArgs args)
    {
        Stop();
        OnPlaybackEnded(sender, args);
    }

    private void OnStartTimeChanged(double? startTime)
    {
        ValidateTimeRange(StartTime, EndTime);
    }

    private void OnEndTimeChanged(double? endTime)
    {
        ValidateTimeRange(StartTime, EndTime);

        Player.PlaybackMediaMarkers.Clear();
        if (endTime.HasValue)
        {
            Player.PlaybackMediaMarkers.Insert(new PlaybackMediaMarker(TimeSpan.FromSeconds(endTime.Value)));
        }
    }

    private void ValidateTimeRange(double? startTime, double? endTime)
    {
        if (startTime.HasValue)
        {
            if (startTime.Value < 0 || startTime.Value > TotalDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(startTime));
            }
        }

        if (endTime.HasValue)
        {
            if (endTime.Value <= 0 || endTime.Value > TotalDuration)
            {
                throw new ArgumentOutOfRangeException(nameof(endTime));
            }
        }

        if (startTime.HasValue && endTime.HasValue)
        {
            if (endTime.Value < startTime.Value)
            {
                throw new ArgumentException("StartTime should less than EndTime");
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_mediaSource == null)
        {
            return;
        }

        if (disposing)
        {
            DeleteMediaSource();
        }

        _isDisposed = true;
    }

    private void DeleteMediaSource()
    {
        if (_mediaSource == null)
            return;

        DeletePlayer();
        _mediaSource.Dispose();
        _mediaSource = null;
    }

    /// <summary>
    /// TODO: Investigate potential crashes:
    /// </summary>
    //~AudioPlayer()
    //{
    //    Dispose(false);
    //}

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
}