using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Views.WaveForm;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Sequencer.ViewModels;

internal class SequencerRecorderViewModel : BaseSequencerViewModel<WaveFormViewModel, ScrollerViewModel>, ISequencerRecorderViewModel
{
    public AudioDetails AudioDetails
    {
        get => Sequencer.InternalRecorder.AudioDetails;
    }

    public ReactiveCommand<Unit, bool>? HasRecordPermissionCommand
    {
        get => Sequencer.HasRecordPermissionCommand;
        set => Sequencer.HasRecordPermissionCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnRecordFailedCommand
    {
        get => Sequencer.OnRecordFailedCommand;
        set => Sequencer.OnRecordFailedCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnRecordDeviceRestoreCommand
    {
        get => Sequencer.OnRecordDeviceRestoreCommand;
        set => Sequencer.OnRecordDeviceRestoreCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnRecordingStartedCommand
    {
        get => Sequencer.OnRecordingStartedCommand;
        set => Sequencer.OnRecordingStartedCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnRecordingFinishedCommand
    {
        get => Sequencer.OnRecordingFinishedCommand;
        set => Sequencer.OnRecordingFinishedCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnEmptyRecordingFinishedCommand
    {
        get => Sequencer.OnEmptyRecordingFinishedCommand;
        set => Sequencer.OnEmptyRecordingFinishedCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnDeleteRecordCommand
    {
        get => Sequencer.OnDeleteRecordCommand;
        set => Sequencer.OnDeleteRecordCommand = value;
    }

    public ReactiveCommand<Unit, Unit>? OnUndoDeleteRecordCommand
    {
        get => Sequencer.OnUndoDeleteRecordCommand;
        set => Sequencer.OnUndoDeleteRecordCommand = value;
    }

    public bool AllowAppendRecordMode
    {
        get => Sequencer.AllowAppendRecordMode;
        set => Sequencer.AllowAppendRecordMode = value;
    }

    [Reactive]
    public bool AppendRecordMode { get; private set; }

    internal SequencerRecorderViewModel(
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder>? recorderFactory,
        FlagType flagType)
        : base(SequencerMode.Recorder, playerFactory, recorderFactory, flagType)

    {
        WaveFormViewModel = new WaveFormViewModel(Sequencer, flagType);
        ScrollerViewModel = new ScrollerViewModel(Sequencer);
    }

    public void SetRecord(RecordAudioModel audio)
    {
        Sequencer.SetAudios(new[] { audio });
    }

    public AudioModel? GetRecord()
    {
        return Sequencer?.CurrentAudio?.Audio;
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        this
            .WhenAnyValue(viewModel => viewModel.Sequencer.AppendRecordMode)
            .BindTo(this, vm => vm.AppendRecordMode)
            .ToDisposables(Disposables);
    }
}