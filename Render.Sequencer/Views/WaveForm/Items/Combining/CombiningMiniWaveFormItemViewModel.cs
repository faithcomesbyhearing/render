using ReactiveUI;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.WaveForm.MiniItems;

namespace Render.Sequencer.Views.WaveForm.Items.Combining;

public class CombiningMiniWaveFormItemViewModel : BaseMiniWaveFormItemViewModel
{
    internal CombiningMiniWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
        : base(audio, sequencer) { }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        SequencerAudio
            .WhenAnyValue(audio => audio.IsCombined)
            .BindTo(this, item => item.IsSelected)
            .ToDisposables(Disposables);
    }
}