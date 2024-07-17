using ReactiveUI;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Services.AudioPlugins.AudioPlayer;
using System.Reactive;
using System.Reactive.Linq;
using Timer = System.Timers.Timer;

namespace Render.Sequencer.Core;

public class InternalPlayer : IDisposable
{
    public const string DefaultAudioName = "Audio 1";
    public const double DefaultTimerIntervalMs = 10;
    public const int SecondsOnScreen = 120;

    private InternalSequencer _sequencer;
    private List<IDisposable> _disposables;

    public double TimerIntervalMs { get; private set; }

    public Timer Timer { get; private set; }

    public Func<IAudioPlayer> PlayerFactory { get; private set; }

    public ReactiveCommand<Unit, Unit> ScrubberDragEndedCommand { get; set; }

    public InternalPlayer(InternalSequencer sequencer, Func<IAudioPlayer> playerFactory)
    {
        _disposables = new();
        _sequencer = sequencer;

        PlayerFactory = playerFactory;
        TimerIntervalMs = DefaultTimerIntervalMs;
        Timer = new Timer(TimerIntervalMs);
        Timer.Elapsed += PlayerTimerTick;

        ScrubberDragEndedCommand = ReactiveCommand.Create(ScrubberDragEnded);
        
        SetupListeners();
    }

    private void SetupListeners()
    {
        _sequencer
            .WhenAnyValue(sequencer => sequencer.IsScrubberDragging)
            .Where(isScrubberDragging => isScrubberDragging && _sequencer.IsPlaying())
            .Subscribe(_ => StopUpdates())
            .ToDisposables(_disposables);
    }

    internal void Play()
    {
        _sequencer.CurrentAudio.Play();
        RunUpdates();

        _sequencer.Mode = SequencerMode.Player;
        _sequencer.State = SequencerState.Playing;
    }

    internal void Pause()
    {
        _sequencer.CurrentAudio.Pause();
        StopUpdates();

        _sequencer.Mode = SequencerMode.Player;
        _sequencer.State = SequencerState.Loaded;
    }

    internal void Stop()
    {
        _sequencer.CurrentAudio.Stop();
        StopUpdates();

        _sequencer.Mode = SequencerMode.Player;
        _sequencer.State = SequencerState.Loaded;
    }
    
    internal void RunUpdates()
    {
        Timer.Start();
    }
    
    internal void StopUpdates()
    {
        Timer.Stop();
    }

    private void ScrubberDragEnded()
    {
        if (_sequencer.IsPlaying())
        {
            UpdatePlayingAudio();
            RunUpdates();
        }
        else
        {
            _sequencer.CurrentAudio?.Seek(_sequencer.CurrentPosition);
        }
    }

    internal void UpdatePlayingAudio()
    {
        if (_sequencer.IsNotPlaying())
        {
            throw new InvalidOperationException(ErrorMessages.AudioIsNotPlaying);
        }

        var audioToSelect = _sequencer.Audios.FindBy(_sequencer.TotalCurrentPosition);
        if (audioToSelect is not null && _sequencer.CurrentAudio != audioToSelect)
        {
            _sequencer.CurrentAudio.Stop();
            _sequencer.CurrentAudio = audioToSelect;
        }

        _sequencer.CurrentPosition = _sequencer.TotalCurrentPosition - _sequencer.CurrentAudio.StartPosition;
        _sequencer.CurrentAudio.Seek(_sequencer.CurrentPosition);
        _sequencer.CurrentAudio.Play();
    }

    private void UpdatePlayingPosition()
    {
        _sequencer.CurrentPosition = _sequencer.CurrentAudio?.CurrentPosition ?? 0;

        _sequencer.TotalCurrentPosition = _sequencer.Audios
            .Take(_sequencer.GetCurrentAudioIndex())
            .Sum(audio => audio.Duration) + _sequencer.CurrentPosition;
    }

    private void PlayerTimerTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        UpdatePlayingPosition();
    }

    public void Dispose()
    {
        Timer.Dispose();
        PlayerFactory = null!;
        
        _disposables.ForEach(disposable => disposable.Dispose());
        _disposables.Clear();
        _disposables = null!;
        _sequencer = null!;
    }
}