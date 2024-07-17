using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Collection;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Sequencer.Views.Flags;
using Render.Sequencer.Views.Flags.Base;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using System.Reactive;
using System.Reactive.Linq;

namespace Render.Sequencer.Core;

public class InternalSequencer : ReactiveObject, IDisposable
{
    private List<IDisposable> _disposables;

    public InternalPlayer InternalPlayer { get; set; }

    public InternalRecorder InternalRecorder { get; set; }

    /// <summary>
    /// Is Sequencer in audio combining mode
    /// </summary>
    public bool IsCombining { get; }

    public FlagType FlagType { get; }

    [Reactive]
    public SequencerMode InitialMode { get; private set; }

    [Reactive]
    public SequencerMode Mode { get; set; }

    [Reactive]
    public SequencerState State { get; set; }

    [Reactive]
    public ObservableRangeCollection<SequencerAudio> Audios { get; private set; }

    [Reactive]
    public SequencerAudio CurrentAudio { get; set; }

    [Reactive]
    public EditableAudioModel? LastDeletedAudio { get; set; }

    [Reactive]
    public int LastDeletedAudioIndex { get; set; } = -1;

    /// <summary>
    /// Take into account, during recording actual total duration 
    /// might be more than value in TotalDuration property because it's calculated by
    /// sum of elapsed Timer ticks. Precise value for TotalDuration evaluetes during 
    /// this.SetAudio() -> Player.Load() methods invocation.
    /// </summary>
    [Reactive]
    public double TotalDuration { get; private set; }

    /// <summary>
    /// Total audio position in seconds, with regards to the whole audios
    /// </summary>
    [Reactive]
    public double TotalCurrentPosition { get; set; }

    [Reactive]
    public bool IsScrubberDragging { get; set; }

    [Reactive]
    public bool IsScrollerDragging { get; set; }

    /// <summary>
    /// Audio position in seconds, with regards to current audio
    /// </summary>
    [Reactive]
    public double CurrentPosition { get; internal set; }

    /// <summary>
    /// Receive values from scroller's slider and
    /// transfer them to ScrollView. 
    /// Transfer values back to slider from OutputScrollX property
    /// </summary>
    [Reactive]
    public double InputScrollX { get; set; }

    /// <summary>
    /// Receive values from ScrollView and
    /// transfer them to scroller's slider
    /// </summary>
    [Reactive]
    public double OutputScrollX { get; set; }

    /// <summary>
    /// Ratio of visible part of main wave forms width to
    /// the whole wave forms width, including scrollable content beyond the edge of the screen
    /// </summary>
    [Reactive]
    public double WidthRatio { get; set; } = -1;

    /// <summary>
    /// Width of the sequencer, including visible area 
    /// and scrollable content beyond the edge of the screen
    /// </summary>
    [Reactive]
    public double TotalWidth { get; set; }

    /// <summary>
    /// Visible width of the sequencer
    /// </summary>
    [Reactive]
    public double Width { get; set; }

    /// <summary>
    /// Possible maximum width of Sequencer control.
    /// Depends on device screen size. Static value.
    /// TODO: React to device display resolution changes.
    /// </summary>
    [Reactive]
    public double MaxWidth { get; set; }

    public int SecondsOnScreen
    {
        get => this.IsInPlayerMode() ? InternalPlayer.SecondsOnScreen : InternalRecorder.SecondsOnScreen;
    }

    [Reactive]
    public bool AllowAppendRecordMode { get; set; }

    [Reactive]
    public bool AppendRecordMode { get; set; }

    [Reactive]
    public bool EditorMode { get; set; }

    [Reactive]
    public bool HasScrubber { get; set; }

    [Reactive]
    public bool IsRightToLeftDirection { get; set; }

    public ObservableRangeCollection<BaseFlagViewModel> Flags { get; private set; }

    public ReactiveCommand<Unit, bool>? HasRecordPermissionCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnRecordFailedCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnRecordDeviceRestoreCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnRecordingStartedCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnRecordingFinishedCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnEmptyRecordingFinishedCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnDeleteRecordCommand { get; set; }

    public ReactiveCommand<Unit, Unit>? OnUndoDeleteRecordCommand { get; set; }

    public ReactiveCommand<IFlag, bool>? AddFlagCommand { get; set; }

    public ReactiveCommand<IFlag, Unit>? TapFlagCommand { get; set; }

    public ReactiveCommand<AudioModel, bool>? TryUnlockAudioCommand { get; set; }

    public ReactiveCommand<Unit, EditableAudioModel?>? InsertRecordCommand { get; set; }

    public event Action? UndoLastAudioCombining;
    public event Func<BaseFlagViewModel, Task<bool>>? RequestNewFlag;

    public InternalSequencer(
        SequencerMode mode,
        FlagType flagType,
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder>? recorderFactory,
        bool isCombining = false,
        bool isEditor = false)
    {
        _disposables = new();

        InternalPlayer = new InternalPlayer(this, playerFactory);
        InternalRecorder = new InternalRecorder(this, recorderFactory);

        Mode = mode;
        FlagType = flagType;
        InitialMode = mode;
        IsCombining = isCombining;
        MaxWidth = /*DeviceDisplay.MainDisplayInfo.Width*/ 1920;
        EditorMode = isEditor;

        Flags = new();
        Audios = new();
        CurrentAudio = SequencerAudio.Empty();

        HasRecordPermissionCommand = ReactiveCommand.Create(() => true);
        TryUnlockAudioCommand = ReactiveCommand.Create((AudioModel _) => false);
        AddFlagCommand = ReactiveCommand.Create((IFlag flag) => true);
        TapFlagCommand = ReactiveCommand.Create((IFlag flag) => { });

        SetAudios(new[] { AudioModel.DefaultEmpty(this.IsRecorder()) });
        SetUpListeners();

        State = SequencerState.Initial;
    }

    private void SetUpListeners()
    {
        this
            .WhenAnyValue(sequencer => sequencer.TotalCurrentPosition)
            .Buffer(2, 1)
            .Where(_ => this.IsLoaded())
            .Where(positionsBuffer => IsScrubberDragging || TapHelper.IsTapped(positionsBuffer[0], positionsBuffer[1]))
            .Subscribe(positionsBuffer => UpdateAudio(currentTotalPosition: positionsBuffer[1]))
            .ToDisposables(_disposables);

        this
            .WhenAnyValue(sequencer => sequencer.OutputScrollX, sequencer => sequencer.WidthRatio)
            .Where(_ => IsScrollerDragging is false)
            .Subscribe(((double OutputScrollX, double WidthRatio) e) => InputScrollX = e.OutputScrollX + 0.5 * Width)
            .ToDisposables(_disposables);

        Audios
            .ToObservableChangeSet()
            .AutoRefresh(audio => audio.Duration)
            .AutoRefresh(audio => audio.IsTemp)
            .ToCollection()
            .Select(audios => audios.Where(audio => audio.IsTemp is false).Sum(audio => audio.Duration))
            .Subscribe(totalDuration => TotalDuration = totalDuration)
            .ToDisposables(_disposables);

        this
            .WhenAnyValue(sequencer => sequencer.AllowAppendRecordMode)
            .Where(_ => AllowAppendRecordMode is false)
            .Subscribe(_ => AppendRecordMode = false)
            .ToDisposables(_disposables);

        this
            .WhenAnyValue(sequencer => sequencer.State, sequencer => sequencer.TotalDuration, sequencer => sequencer.AppendRecordMode)
            .Where(_ => this.IsNotRecording())
            .Subscribe(SetScrubberState)
            .ToDisposables(_disposables);
    }

    internal void SetAudios(AudioModel[] audios)
    {
        this.StopActivities();

        State = SequencerState.Initial;

        var sequencerAudios = this.CreateSequencerAudios(audios, InternalPlayer.PlayerFactory);
        var currentAudio = sequencerAudios[0];

        Audios.ForEach(audio => audio.Dispose());
        Audios.ReplaceRange(sequencerAudios);
        CurrentAudio = currentAudio;

        Mode = this.IsRecorder() && currentAudio.IsTempOrEmpty ? SequencerMode.Recorder : SequencerMode.Player;
        State = SequencerState.Loaded;

        InitializeFlags(sequencerAudios);
        this.SetChainedPlayback(sequencerAudios);
        this.ForceScrubberToStart();
        this.ForceScrollerToStart();
    }

    internal void SetEditorAudios(EditableAudioModel[] audios)
    {
        this.StopActivities();

        State = SequencerState.Initial;

        var sequencerAudios = this.CreateSequencerAudios(audios, InternalPlayer.PlayerFactory);
        var currentAudio = sequencerAudios[0];

        Audios.ForEach(audio => audio.Dispose());
        Audios.ReplaceRange(sequencerAudios);
        CurrentAudio = currentAudio;

        Mode = this.IsRecorder() && currentAudio.IsTempOrEmpty ? SequencerMode.Recorder : SequencerMode.Player;
        State = SequencerState.Loaded;

        this.SetChainedPlayback(sequencerAudios);
    }

    internal void SetCombinedAudios(CombinableAudioModel[] audios)
    {
        this.StopActivities();

        State = SequencerState.Initial;

        var sequencerAudios = this.CreateSequencerAudios(audios, InternalPlayer.PlayerFactory);
        var currentAudio = sequencerAudios.First(sequencerAudio => sequencerAudio.Audio.IsBase);

        Audios.ForEach(audio => audio.Dispose());
        Audios.ReplaceRange(sequencerAudios);
        CurrentAudio = currentAudio;
        CurrentAudio.PlaybackEnded += this.GetPlaybackEndedCallback();

        Mode = SequencerMode.Player;
        State = SequencerState.Loaded;

        this.ForceScrollerToStart();
        this.ForceScrubberTo(CurrentAudio.StartPosition);
    }

    private void InitializeFlags(SequencerAudio[] sequencerAudios)
    {
        if (FlagType is FlagType.None)
        {
            return;
        }

        var newFlags = sequencerAudios
            .Where(audio => audio.Audio is not null)
            .SelectMany(audio =>
        {
            var audioHashCode = audio.GetHashCode();
            return audio.Audio.Flags.Select(flag => flag.ToViewModel(audio.StartPosition, audioHashCode));
        });

        Flags.ReplaceRange(newFlags.ToList());
    }

    internal void UpdateAudio(double currentTotalPosition)
    {
        if (this.IsPlaying())
        {
            throw new InvalidOperationException(ErrorMessages.AudioIsPlaying);
        }

        TryUpdateCurrentAudio(currentTotalPosition);
        
        CurrentPosition = currentTotalPosition - CurrentAudio.StartPosition;
    }

    internal void ChangeCurrentAudio(SequencerAudio audio)
    {
        CurrentAudio = audio;
        CurrentAudio.Seek(0);
        CurrentPosition = 0;
        TotalCurrentPosition = CurrentAudio.StartPosition;
    }

    private void TryUpdateCurrentAudio(double currentTotalPosition)
    {
        if (currentTotalPosition >= CurrentAudio.StartPosition && 
            currentTotalPosition <= CurrentAudio.EndPosition)
        {
            return;
        }

        var audioToSelect = Audios.FindBy(currentTotalPosition);
        if (audioToSelect is not null && CurrentAudio != audioToSelect)
        {
            CurrentAudio?.Seek(0);
            CurrentAudio = audioToSelect;
        }
    }

    internal async Task AddFlag()
    {
        if (this.IsRecording() || this.IsPlaying() || RequestNewFlag is null || FlagType is FlagType.None)
        {
            return;
        }

        BaseFlagViewModel flagViewModel = FlagType switch
        {
            FlagType.Marker => new MarkerFlagViewModel
            {
                Symbol = 0.ToString(),
                AudioHashCode = CurrentAudio.GetHashCode(),
                AbsPositionSec = CurrentAudio.StartPosition + CurrentAudio.CurrentPosition,
                PositionSec = CurrentAudio.CurrentPosition,
            },
            FlagType.Note => new NoteFlagViewModel
            {
                AudioHashCode = CurrentAudio.GetHashCode(),
                AbsPositionSec = CurrentAudio.StartPosition + CurrentAudio.CurrentPosition,
                PositionSec = CurrentAudio.CurrentPosition,
            },
            FlagType.None => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };

        var isSuccess = await RequestNewFlag.Invoke(flagViewModel);
        if (isSuccess && IsFlagExists(flagViewModel.Key) is false)
        {
            CurrentAudio.AddFlag(flagViewModel.ToFlagModel());
            Flags.Add(flagViewModel);
        }
    }

    public void AddFlag(FlagModel flag)
    {
        CurrentAudio.AddFlag(flag);
        Flags.Add(flag.ToViewModel(CurrentAudio.StartPosition, CurrentAudio.GetHashCode()));
    }

    public IFlag? GetFlag(Guid key)
    {
        return Flags.FirstOrDefault(flag => flag.Key == key);
    }

    public void RemoveFlag(IFlag flag)
    {
        var flagToRemove = Flags.FirstOrDefault(f => f.Key == flag.Key);
        if (flagToRemove is null)
        {
            return;
        }

        Flags.Remove(flagToRemove);

        foreach(var audio in Audios)
        {
            var removed = audio.RemoveFlag(flagToRemove.Key);
            if (removed)
            {
                break;
            }
        }
    }

    public bool IsFlagExists(Guid key)
    {
        return Flags.Any(flag => flag.Key == key);
    }

    internal void UndoLastCombinedAudio()
    {
        UndoLastAudioCombining?.Invoke();
    }

    internal void CutAudio()
    {
        if (CurrentAudio is not EditableSequencerAudio audio || audio.CanCut is false)
        {
            return;
        }

        SetLastDeletedAudio(null);

        var split = audio.Split(CurrentPosition);
        var audios = Audios
            .SelectMany(audio => audio.Equals(CurrentAudio) ? split : new[] { audio.Audio })
            .Cast<EditableAudioModel>()
            .ToArray();

        SetEditorAudios(audios);
        CurrentAudio = Audios.First(audio => audio.Audio == split.Last());
        this.ForceScrubberTo(CurrentAudio.StartPosition);
    }

    internal void DeleteAudio()
    {
        if (CurrentAudio is null)
        {
            return;
        }

        SetLastDeletedAudio(CurrentAudio);

        var audios = Audios
            .Except(new[] { CurrentAudio })
            .Select(audio => audio.Audio)
            .Cast<EditableAudioModel>()
            .ToArray();

        if(audios.Length == 0)
        {
            audios = new[] { EditableAudioModel.DefaultEmpty() }; 
        }

        SetEditorAudios(audios);
        CurrentAudio = LastDeletedAudioIndex < Audios.Count ? Audios[LastDeletedAudioIndex] : Audios.Last();
        this.ForceScrubberTo(CurrentAudio.StartPosition);
    }

    internal void UndoDeleteAudio()
    {
        if (LastDeletedAudio is null)
        {
            return;
        }

        var audios = Audios
            .Select(audio => audio.Audio)
            .Cast<EditableAudioModel>()
            .ToList();

        if(audios.Count == 1 && audios[0].IsEmpty)
        {
            audios = new() { LastDeletedAudio };
        }
        else
        {
            audios.Insert(LastDeletedAudioIndex, LastDeletedAudio);
        }

        SetEditorAudios(audios.ToArray());
        CurrentAudio = Audios[LastDeletedAudioIndex];
        this.ForceScrubberTo(CurrentAudio.StartPosition);
        SetLastDeletedAudio(null);
    }

    internal async Task InsertAudioAsync()
    {
        if (CurrentAudio is not EditableSequencerAudio audio)
        {
            return;
        }

        var insertedAudio = await InsertRecordCommand.SafeExecute();
        if (insertedAudio is null)
        {
            return;
        }

        EditableAudioModel[] changedAudios;

        if (audio.CanCut)
        {
            var split = audio.Split(CurrentPosition);
            changedAudios = new[] { split[0], insertedAudio, split[1] };
        }
        else
        {
            changedAudios = audio.AppendOrPrependAudio(CurrentPosition, insertedAudio);
        }

        var audios = Audios
            .SelectMany(audio => audio.Equals(CurrentAudio) ? changedAudios : new[] { audio.Audio })
            .Cast<EditableAudioModel>()
            .ToArray();

        SetEditorAudios(audios);
        CurrentAudio = Audios.First(audio => audio.Audio == insertedAudio);
        LastDeletedAudio = null;
        this.ForceScrubberTo(CurrentAudio.StartPosition);
    }

    private void SetLastDeletedAudio(SequencerAudio? deletedAudio)
    {
        if (deletedAudio is null)
        {
            LastDeletedAudioIndex = -1;
            LastDeletedAudio = null;
        }
        else
        {
            LastDeletedAudioIndex = deletedAudio is null ? -1 : Audios.IndexOf(deletedAudio);
            LastDeletedAudio = deletedAudio?.Audio as EditableAudioModel;
        }
    }

    private void SetScrubberState((SequencerState State, double TotalDuration, bool AppendRecordMode) options)
    {
        HasScrubber = options.TotalDuration is not 0d &&
                      options.State is not SequencerState.Recording &&
                      options.AppendRecordMode is not true;
    }

    /// <summary>
    /// TODO: Push error to the client code 
    /// to display notification to the user
    /// </summary>
    internal void PushError() { }

    public void Dispose()
    {
        InternalPlayer?.Dispose();
        InternalPlayer = null!;

        InternalRecorder?.Dispose();
        InternalRecorder = null!;

        HasRecordPermissionCommand?.Dispose();
        HasRecordPermissionCommand = null;
        OnRecordFailedCommand?.Dispose();
        OnRecordFailedCommand = null;
        OnRecordDeviceRestoreCommand?.Dispose();
        OnRecordDeviceRestoreCommand = null;
        OnRecordingStartedCommand?.Dispose();
        OnRecordingStartedCommand = null;
        OnRecordingFinishedCommand?.Dispose();
        OnRecordingFinishedCommand = null;
        OnEmptyRecordingFinishedCommand?.Dispose();
        OnEmptyRecordingFinishedCommand = null;
        OnDeleteRecordCommand?.Dispose();
        OnDeleteRecordCommand = null;
        OnUndoDeleteRecordCommand?.Dispose();
        OnUndoDeleteRecordCommand = null;
        AddFlagCommand?.Dispose();
        AddFlagCommand = null;
        TapFlagCommand?.Dispose();
        TapFlagCommand = null;
        TryUnlockAudioCommand?.Dispose();
        TryUnlockAudioCommand = null;
        InsertRecordCommand?.Dispose();
        InsertRecordCommand = null;

        RequestNewFlag = null;
        UndoLastAudioCombining = null;

        _disposables.ForEach(disposable => disposable.Dispose());
        _disposables.Clear();
        _disposables = null!;

        Audios.ForEach(audio => audio.Dispose());
        Audios.ClearSilent();
        Audios = null!;
        CurrentAudio = null!;
    }
}