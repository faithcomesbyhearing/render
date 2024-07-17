using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Views.WaveForm.MiniItems;
using Render.Sequencer.Views.WaveForm.Items.Editable;
using ReactiveUI;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Scroller;

public class EditableScrollerViewModel : BaseScrollerViewModel
{
    public EditableScrollerViewModel(InternalSequencer sequencer)
        : base(sequencer) { }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.HasScrubber)
            .BindTo(this, scroller => scroller.HasScrubber)
            .ToDisposables(Disposables);
    }

    protected override BaseMiniWaveFormItemViewModel CreateMiniWaveFormViewModel(SequencerAudio audio)
    {
        return new EditableMiniWaveFormItemViewModel(audio, Sequencer);
    }
}