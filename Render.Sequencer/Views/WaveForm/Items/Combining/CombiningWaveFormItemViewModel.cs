using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.WaveForm.Items.Combining;

public class CombiningWaveFormItemViewModel : BaseWaveFormItemViewModel
{
    private enum InsertPosition { Start, End };

    private int _combinedItemsCount = 0;
    private double _currentPositionOffset = 0;

    [Reactive]
    public string? StartIcon { get; set; }

    [Reactive]
    public string Name { get; set; }

    [Reactive]
    public string? Number { get; set; }

    [Reactive]
    public double CurrentPosition { get; set; }

    [Reactive]
    public double StartPosition { get; set; }

    [Reactive]
    public double EndPosition { get; set; }

    [Reactive]
    public ObservableCollection<CombinableWaveFormItemViewModel> CombinableWaveFormItems { get; set; }

    internal CombiningWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
        : base(sequencer)
    {
        audio.IsCombined = true;
        CombinableWaveFormItems = new() { new CombinableWaveFormItemViewModel(audio, sequencer, null, null) };

        Name = audio.Audio.GetFullName();
        Number = audio.Audio.AudioNumber;
        StartIcon = audio.Audio.StartIcon;
        StartPosition = audio.StartPosition;
        EndPosition = audio.EndPosition;
        Duration = audio.Duration;

        IsSelected = true;
    }

    internal void Prepend(CombinableWaveFormItemViewModel item)
    {
        Insert(item, InsertPosition.Start);
        UpdateCombinedAudioName();
    }

    internal void Append(CombinableWaveFormItemViewModel item)
    {
        Insert(item, InsertPosition.End);
    }

    internal CombinableWaveFormItemViewModel? RemoveLastCombined()
    {
        if (CombinableWaveFormItems.Count is 1)
        {
            return null;
        }

        var lastCombinedItem = CombinableWaveFormItems
            .OrderBy(item => item.CombineOrder)
            .Last();

        CombinableWaveFormItems.Remove(lastCombinedItem);

        UpdateAudios();
        UpdateCombinedAudioName();

        --_combinedItemsCount;
        return lastCombinedItem;
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentPosition)
            .Subscribe(UpdateCurrentPosition)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentAudio)
            .Subscribe(UpdatePositionOffset)
            .ToDisposables(Disposables);
    }

    private void UpdateCurrentPosition(double currentAudioPosition)
    {
        CurrentPosition = currentAudioPosition + _currentPositionOffset;
    }

    private void UpdatePositionOffset(SequencerAudio currentAudio)
    {
        var combinableItem = CombinableWaveFormItems?.FirstOrDefault(item => item.SequencerAudio == currentAudio);
        if (combinableItem is null)
        {
            return;
        }

        _currentPositionOffset = CombinableWaveFormItems!
            .Take(CombinableWaveFormItems!.IndexOf(combinableItem))
            .Sum(item => item.SequencerAudio.Duration);
    }

    private void Insert(CombinableWaveFormItemViewModel item, InsertPosition position)
    {
        var index = position switch
        {
            InsertPosition.Start => 0,
            InsertPosition.End => CombinableWaveFormItems.Count,
            _ => throw new NotImplementedException(),
        };

        item.ModifyState(canCombine: false, canUnlock: false);
        item.MakeCombined(++_combinedItemsCount);

        CombinableWaveFormItems.Insert(index, item);
        UpdateAudios();
    }

    private void UpdateAudios()
    {
        CombinableWaveFormItems.ForEach(item =>
        {
            item.SequencerAudio.Seek(0);
            item.SequencerAudio.RemovePlaybackEndedHandlers();
        });
        Sequencer.SetChainedPlayback(CombinableWaveFormItems.Select(item => item.SequencerAudio).ToArray());

        StartPosition = CombinableWaveFormItems.First().SequencerAudio.StartPosition;
        EndPosition = CombinableWaveFormItems.Last().SequencerAudio.EndPosition;
        Duration = EndPosition - StartPosition;

        Sequencer.ChangeCurrentAudio(CombinableWaveFormItems.First().SequencerAudio);
    }

    private void UpdateCombinedAudioName()
    {
        Name = CombinableWaveFormItems[0].SequencerAudio.Audio.GetFullName();
        Number = CombinableWaveFormItems[0].SequencerAudio.Audio.AudioNumber;
    }
}