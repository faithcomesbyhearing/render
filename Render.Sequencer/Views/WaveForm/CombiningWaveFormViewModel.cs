using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using DynamicData;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.WaveForm.Items.Combining;
using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Views.WaveForm;

internal class CombiningWaveFormViewModel : BaseWaveFormViewModel
{
    private ReactiveCommand<CombinableWaveFormItemViewModel, Unit> _unlockItemCommand;
    private ReactiveCommand<CombinableWaveFormItemViewModel, Unit> _combineItemCommand;

    public int CombiningItemIndex
    {
        get => WaveFormItems.IndexOf(CombiningItem!);
    }

    public CombiningWaveFormItemViewModel? CombiningItem { get; private set; }

    public CombiningWaveFormViewModel(InternalSequencer sequencer)
        : base(sequencer)
    {
        _unlockItemCommand = ReactiveCommand.CreateFromTask<CombinableWaveFormItemViewModel>(TryUnlockItem, IsAvailable());
        _combineItemCommand = ReactiveCommand.Create<CombinableWaveFormItemViewModel>(CombineItem, IsAvailable());

        HasScrubber = false;

        Sequencer.UndoLastAudioCombining += RemoveLastCombinedItem;
    }

    internal override bool TrySelectWaveForm(int _)
    {
        return false;
    }

    private IObservable<bool> IsAvailable()
    {
        return Sequencer
            .WhenAnyValue(sequencer => sequencer.State)
            .Select(state => state is SequencerState.Loaded)
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private async Task TryUnlockItem(CombinableWaveFormItemViewModel item)
    {
        var canUnlock = item.CanUnlock && await Sequencer.TryUnlockAudioCommand.SafeExecute(item.SequencerAudio.Audio);
        if (canUnlock)
        {
            var currentItemIndex = WaveFormItems.IndexOf(item);
            var canCombine = Math.Abs(currentItemIndex - CombiningItemIndex) == 1;

            item.TryUnlock();
            item.ModifyState(canCombine, canUnlock: false);
        }
    }

    private void CombineItem(CombinableWaveFormItemViewModel item)
    {
        var combinedItemIndex = WaveFormItems.IndexOf(item);
        var isLeftItem = combinedItemIndex < CombiningItemIndex;
        var nextOrPreviousItem = isLeftItem ? WaveFormItems.GetPreviousItemOf(item) : WaveFormItems.GetNextItemOf(item);

        nextOrPreviousItem?
            .As<CombinableWaveFormItemViewModel>()!
            .ModifyState(canCombine: true, canUnlock: true);

        if (isLeftItem)
        {
            CombiningItem!.Prepend(item);
        }
        else
        {
            CombiningItem!.Append(item);
        }

        WaveFormItems.Remove(item);
        RenumerateItems();
    }

    private void RemoveLastCombinedItem()
    {
        var lastCombinedItem = CombiningItem!.RemoveLastCombined();
        if (lastCombinedItem is null)
        {
            return;
        }

        lastCombinedItem.TryMakeCombinable();
        if (lastCombinedItem.SequencerAudio.StartPosition >= CombiningItem!.EndPosition)
        {
            WaveFormItems
                .GetNextItemOf(CombiningItem)?
                .As<CombinableWaveFormItemViewModel>()!
                .Reset();

            WaveFormItems.Insert(CombiningItemIndex + 1, lastCombinedItem);
        }
        else
        {
            WaveFormItems
                .GetPreviousItemOf(CombiningItem)?
                .As<CombinableWaveFormItemViewModel>()!
                .Reset();

            WaveFormItems.Insert(CombiningItemIndex, lastCombinedItem);
        }

        RenumerateItems();
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

        CreateCombiningWaveFormItems(audios);
    }

    private void CreateCombiningWaveFormItems(SequencerAudio[] audios)
    {
        for (int i = 0; i < audios.Length; i++)
        {
            var audio = audios[i];

            if (audio.Audio.IsBase)
            {
                CombiningItem = new CombiningWaveFormItemViewModel(audio, Sequencer);
                WaveFormItems.Add(CombiningItem);

                continue;
            }

            WaveFormItems.Add(new CombinableWaveFormItemViewModel(audio, Sequencer, _unlockItemCommand, _combineItemCommand));
        }

        if (WaveFormItems.Count is 1)
        {
            return;
        }

        var previousItem = (CombinableWaveFormItemViewModel?)null;
        var nextItem = (CombinableWaveFormItemViewModel?)null;
        var combiningItemIndex = CombiningItemIndex;

        if (combiningItemIndex > 0)
        {
            previousItem = (CombinableWaveFormItemViewModel)WaveFormItems[combiningItemIndex - 1];
        }

        if (combiningItemIndex < WaveFormItems.Count - 1)
        {
            nextItem = (CombinableWaveFormItemViewModel)WaveFormItems[combiningItemIndex + 1];
        }

        nextItem?.ModifyState(canCombine: true, canUnlock: true);
        previousItem?.ModifyState(canCombine: true, canUnlock: true);
    }

    private void RenumerateItems()
    {
        for (int i = CombiningItemIndex + 1; i < WaveFormItems.Count; i++)
        {
            if (WaveFormItems[i] is CombinableWaveFormItemViewModel combinableItem)
            {
                combinableItem.Renumerate(i + 1);
            }
        }
    }

    public override void Dispose()
    {
        Sequencer.UndoLastAudioCombining -= RemoveLastCombinedItem;

        base.Dispose();
    }
}