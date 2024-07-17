using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.WaveForm.MiniItems;
using System.Reactive.Linq;

namespace Render.Sequencer.Views.WaveForm.Items.Editable;

public class EditableMiniWaveFormItemViewModel : BaseMiniWaveFormItemViewModel
{
    [Reactive]
    public bool IsEditableSelected { get; private set; }

    internal EditableMiniWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
        : base(audio, sequencer) 
    {

    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentAudio)
            .Select(currentAudio => currentAudio == SequencerAudio)
            .BindTo(this, item => item.IsSelected)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentAudio)
            .Select(currentAudio => currentAudio == SequencerAudio)
            .BindTo(this, item => item.IsEditableSelected)
            .ToDisposables(Disposables);
    }
}