using ReactiveUI;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Views.WaveForm.Items.Combining;
using Render.Sequencer.Views.WaveForm.MiniItems;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Scroller;

public class CombiningScrollerViewModel : BaseScrollerViewModel
{
    public CombiningScrollerViewModel(InternalSequencer sequencer)
        : base(sequencer) { }

    protected override BaseMiniWaveFormItemViewModel CreateMiniWaveFormViewModel(SequencerAudio audio)
    {
        return new CombiningMiniWaveFormItemViewModel(audio, Sequencer);
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.HasScrubber)
            .BindTo(this, scroller => scroller.HasScrubber)
            .ToDisposables(Disposables);
    }
}