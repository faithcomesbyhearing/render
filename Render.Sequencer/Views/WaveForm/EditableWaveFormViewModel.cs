using System.Reactive.Linq;
using ReactiveUI;
using DynamicData;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.WaveForm.Items.Editable;

namespace Render.Sequencer.Views.WaveForm;

internal class EditableWaveFormViewModel : BaseWaveFormViewModel
{
    public EditableWaveFormViewModel(InternalSequencer sequencer)
        : base(sequencer) { }

    internal override bool TrySelectWaveForm(int _)
    {
        return false;
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentAudio)
            .Subscribe(SelectWaveFormItem)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.HasScrubber)
            .BindTo(this, waveForm => waveForm.HasScrubber)
            .ToDisposables(Disposables);
    }

    protected override void ReplaceWaveForms(IChangeSet<SequencerAudio> audiosChange)
    {
        var audios = audiosChange
                        .Where(change => change.Reason is ListChangeReason.AddRange)
                        .SelectMany(change => change.Range.ToArray())
                        .ToArray();

        WaveFormItems.ForEach(item => item.Dispose());
        WaveFormItems.Clear();

        if (Sequencer.IsPlayer() && audios.IsEmptyAudios())
        {
            return;
        }

        audios.ForEach(audio => WaveFormItems.Add(new EditableWaveFormItemViewModel(audio, Sequencer)));
    }

    private void SelectWaveFormItem(SequencerAudio currentAudio)
    {
        var selectedItem = WaveFormItems.FirstOrDefault(item => item.SequencerAudio == currentAudio);
        if (selectedItem is null)
        {
            return;
        }

        SelectedItem = selectedItem;
        WaveFormItems.ForEach(item => item.IsSelected = SelectedItem == item);
    }
}