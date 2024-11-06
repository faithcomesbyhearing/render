using System.Reactive.Linq;
using DynamicData.Binding;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.WaveForm.Items;
using Render.Sequencer.Core.Utils.Collection;

namespace Render.Sequencer.Views.WaveForm;

public abstract class BaseWaveFormViewModel : BaseViewModel
{
    [Reactive]
    public bool HasScrubber { get; set; }

    [Reactive]
    public InternalSequencer Sequencer { get; private set; }

    [Reactive]
    public BaseWaveFormItemViewModel? SelectedItem { get; set; }

    public ObservableRangeCollection<BaseWaveFormItemViewModel> WaveFormItems { get; set; }

    protected BaseWaveFormViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;
        WaveFormItems = new();

        SetupListeners();
    }

    internal virtual bool TrySelectWaveForm(int index)
    {
        var itemToSelect = WaveFormItems.GetOrDefault(index);
        if (itemToSelect is not null)
        {
            Sequencer.ChangeCurrentAudio(itemToSelect.SequencerAudio);

            return true;
        }

        return false;
    }

    protected virtual void SetupListeners()
    {
        Sequencer.Audios
            .ToObservableChangeSet()
            .Where(_ => Sequencer.IsNotRecording())
            .Transform(audio => audio)
            .Subscribe(ReplaceWaveForms)
            .ToDisposables(Disposables);
    }

    protected abstract void ReplaceWaveForms(IChangeSet<SequencerAudio> audiosChange);

    public override void Dispose()
    {
        WaveFormItems.ForEach(item => item.Dispose());
        WaveFormItems.ClearSilent();
        SelectedItem = null;

        base.Dispose();
    }
}