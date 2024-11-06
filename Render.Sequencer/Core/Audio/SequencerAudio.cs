using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Sequencer.Core.Audio;

public class SequencerAudio : ReactiveObject, IDisposable
{
    public static SequencerAudio Empty()
    {
        return new SequencerAudio();
    }

    public IAudioPlayer Player { get; private set; }

    /// <summary>
    /// Audio duration is seconds
    /// </summary>
    [Reactive]
    public double Duration { get; set; }

    /// <summary>
    /// Start position in seconds
    /// </summary>
    public double StartPosition { get; private set; }

    /// <summary>
    /// End position in seconds
    /// </summary>
    public double EndPosition
    {
        get => StartPosition + Duration;
    }

    /// <summary>
    /// Current position in seconds
    /// </summary>
    public double CurrentPosition
    {
        get => Player.CurrentPosition;
    }

    /// <summary>
    /// In combine mode, audio can be merged with another.
    /// This property tells whether audio is combined or not.
    /// </summary>
    [Reactive]
    public bool IsCombined { get; set; }

    /// <summary>
    /// Is audio marked as temporary deleted
    /// </summary>
    [Reactive]
    public bool IsTemp { get; private set; }

    /// <summary>
    /// Is audio marked as temporary deleted or empty
    /// </summary>
    public bool IsTempOrEmpty => IsTemp || this.HasAudioData() is false;

    /// <summary>
    /// Total samples for audio in player mode.
    /// Visible samples on the screen (~30 sec) in recording mode. 
    /// </summary>
    [Reactive]
    internal float[] LastSamples { get; set; }

    [Reactive]
    internal float[] TotalSamples { get; set; }

    [Reactive]
    public AudioModel Audio { get; private set; }

    public event EventHandler? PlaybackEnded;

    private SequencerAudio()
    {
        Audio = AudioModel.DefaultEmpty(false);
        Player = null!;
        LastSamples = Array.Empty<float>();
        TotalSamples = Array.Empty<float>();
    }

    internal SequencerAudio(AudioModel audio, Func<IAudioPlayer> playerFactory)
    {
        Audio = audio;
        Player = playerFactory();
        IsTemp = audio.IsTemp;
        LastSamples = Array.Empty<float>();
        TotalSamples = Array.Empty<float>();

        Player.PlaybackEnded += (s, a) => PlaybackEnded?.Invoke(s, a);
    }

    internal virtual bool TrySetAudio(AudioModel audio, double startPosition)
    {
        Audio = audio;
        IsTemp = audio.IsTemp;

        Player.Unload();
        return Init(startPosition);
    }

    internal void RemoveAudioData()
    {
        Audio = AudioModel.Empty(Audio.Name);
        LastSamples = Array.Empty<float>();
        TotalSamples = Array.Empty<float>();
        Player.Unload();

        IsTemp = false;
        Duration = 0;
        StartPosition = 0;
    }

    internal virtual bool Init(double startPosition)
    {
        bool isSuccess;
        if (Audio.IsFileExists)
        {
            isSuccess = Player.Load(new FileStream(Audio.Path!, FileMode.Open, FileAccess.Read));
        }
        else if (Audio.HasData)
        {
            isSuccess = Player.Load(new MemoryStream(Audio.Data!));
        }
        else
        {
            return false;
        }

        Duration = Player.Duration;
        StartPosition = startPosition;

        return isSuccess;
    }

    /// <summary>
    /// Updates duration while recording.
    /// </summary>
    internal void SetDuration(double duration)
    {
        Duration = duration;
    }

    internal void Play()
    {
        Player.Play();
    }

    internal void Pause()
    {
        Player.Pause();
    }

    internal void Stop()
    {
        Player.Stop();
    }

    internal void Unload()
    {
        Player?.Unload();
        
        Duration = 0;
        StartPosition = 0;
        LastSamples = Array.Empty<float>();
        TotalSamples = Array.Empty<float>();
        Audio = AudioModel.Create(Array.Empty<byte>(), InternalRecorder.DefaultRecordName);
    }

    internal void Seek(double position)
    {
        Player.Seek(position);
    }

    internal async void RefreshPlayerSamples(BuildSamplesParams buildParams)
    {
        var audioSamples = await SamplesHelper.BuildPlayerSamplesAsync(Audio, buildParams);

        LastSamples = audioSamples.LastSamples;
        TotalSamples = audioSamples.TotalSamples;

        CommitSamples();
    }

    internal void RefreshRecorderSamples(BuildSamplesParams buildParams)
    {
        var audioSamples = SamplesHelper.BuildRecorderSamples(Audio, buildParams);

        LastSamples = audioSamples.LastSamples;
        TotalSamples = audioSamples.TotalSamples;

        CommitSamples();
    }

    internal void RefreshRecorderSamples(byte[] dataChunk, BuildSamplesParams buildParams)
    {
        var audioSamples = SamplesHelper.BuildInMemoryRecorderSamples(dataChunk, buildParams, TotalSamples);

        LastSamples = audioSamples.LastSamples;
        TotalSamples = audioSamples.TotalSamples;

        CommitSamples();
    }

    internal void AddFlag(FlagModel flag)
    {
        Audio.Flags.Add(flag);
    }

    internal bool RemoveFlag(Guid key)
    {
        var flagToRemove = Audio.Flags.Find(flag => flag.Key == key);
        if (flagToRemove is null)
        {
            return false;
        }

        return Audio.Flags.Remove(flagToRemove);
    }

    internal void MakeTemp()
    {
        Seek(0);
        LastSamples = TotalSamples = Array.Empty<float>();
        IsTemp = true;
    }

    internal void MakeAvailable()
    {
        IsTemp = false;
    }

    internal void RemovePlaybackEndedHandlers()
    {
        PlaybackEnded = null;
    }

    internal void CommitSamples()
    {
        if (Audio is not null)
        {
            Audio.Samples = TotalSamples;
        }
    }

    public void Dispose()
    {
        RemovePlaybackEndedHandlers();

        Audio = null!;
        TotalSamples = LastSamples = Array.Empty<float>();

        Player?.Dispose();
        Player = null!;
    }
}