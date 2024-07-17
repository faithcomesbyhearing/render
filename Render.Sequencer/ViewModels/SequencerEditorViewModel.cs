using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Views.WaveForm;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Contracts.Enums;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Helpers;
using ReactiveUI;
using System.Reactive;
using Render.Models.Audio;

namespace Render.Sequencer.ViewModels;

internal class SequencerEditorViewModel : BaseSequencerViewModel<EditableWaveFormViewModel, EditableScrollerViewModel>, ISequencerEditorViewModel
{
    public AudioDetails AudioDetails
    {
        get => Sequencer.InternalRecorder.AudioDetails;
    }

    public ReactiveCommand<Unit, EditableAudioModel?>? InsertRecordCommand
    {
        get => Sequencer.InsertRecordCommand;
        set => Sequencer.InsertRecordCommand = value;
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

    public SequencerEditorViewModel(Func<IAudioPlayer> playerFactory, Func<IAudioRecorder>? recorderFactory) 
        : base(SequencerMode.Recorder, playerFactory, recorderFactory, FlagType.None, isEditor: true)
    {
        WaveFormViewModel = new EditableWaveFormViewModel(Sequencer);
        ScrollerViewModel = new EditableScrollerViewModel(Sequencer);

        InsertRecordCommand = ReactiveCommand.Create(() => (EditableAudioModel?)null);
    }

    public void SetRecord(EditableAudioModel audio)
    {
        Sequencer.SetEditorAudios(new[] { audio } );
    }

    public EditableAudioModel GetRecord()
    {
        var audios = Sequencer.Audios.Select(audio => audio.Audio).Cast<EditableAudioModel>().ToArray();
        var mergeResult = WavHelper.MergeWavFiles(audios);

        return EditableAudioModel.Create(mergeResult.MergedFilePath!);
    }
}
