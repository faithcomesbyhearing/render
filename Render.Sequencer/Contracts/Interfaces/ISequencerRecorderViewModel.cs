using ReactiveUI;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Models;
using System.Reactive;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerRecorderViewModel : ISequencerViewModel, ISequencerCommonRecorderViewModel
{
    bool AppendRecordMode { get; } 
    bool AllowAppendRecordMode { get; set; }

    ReactiveCommand<Unit, Unit>? OnRecordingStartedCommand { get; set; }
    ReactiveCommand<Unit, Unit>? OnRecordingFinishedCommand { get; set; }
    ReactiveCommand<Unit, Unit>? OnEmptyRecordingFinishedCommand { get; set; }
    ReactiveCommand<Unit, Unit>? OnDeleteRecordCommand { get; set; }
    ReactiveCommand<Unit, Unit>? OnUndoDeleteRecordCommand { get; set; }

    AudioModel? GetRecord();

    void SetRecord(RecordAudioModel audio);
}