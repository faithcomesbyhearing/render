using ReactiveUI;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Models;
using System.Reactive;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerEditorViewModel : ISequencerViewModel, ISequencerCommonRecorderViewModel
{
    ReactiveCommand<Unit, EditableAudioModel?>? InsertRecordCommand { get; set; }

    void SetRecord(EditableAudioModel audio);
    EditableAudioModel GetRecord();
}
