using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;

namespace Render.Sequencer.Views.WaveForm.Items.Editable;

public class EditableWaveFormItemViewModel : BaseWaveFormItemViewModel
{
    internal EditableWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
        : base(audio, sequencer) { }
}