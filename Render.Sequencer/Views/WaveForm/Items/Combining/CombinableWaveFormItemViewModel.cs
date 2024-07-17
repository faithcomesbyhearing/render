using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.WaveForm.Items.Combining;

public class CombinableWaveFormItemViewModel : BaseWaveFormItemViewModel
{
    public int CombineOrder { get; private set; }

    [Reactive]
    public string Name { get; set; }

    [Reactive]
    public string? Number { get; set; }

    [Reactive]
    public bool IsLocked { get; set; }

    [Reactive]
    public bool CanCombine { get; set; }

    [Reactive]
    public bool CanUnlock { get; set; }

    [Reactive]
    public bool IsAvailable { get; set; }

    [Reactive]
    public ReactiveCommand<CombinableWaveFormItemViewModel, Unit>? UnlockItemCommand { get; set; }

    [Reactive]
    public ReactiveCommand<CombinableWaveFormItemViewModel, Unit>? CombineItemCommand { get; set; }

    internal CombinableWaveFormItemViewModel(
        SequencerAudio audio,
        InternalSequencer sequencer,
        ReactiveCommand<CombinableWaveFormItemViewModel, Unit>? unlockItemCommand,
        ReactiveCommand<CombinableWaveFormItemViewModel, Unit>? combineItemCommand)
        : base(audio, sequencer)
    {
        Name = audio.Audio.GetFullName();
        Number = audio.Audio.AudioNumber;
        IsLocked = audio.Audio.IsInitialyLocked;
        UnlockItemCommand = unlockItemCommand;
        CombineItemCommand = combineItemCommand;
    }

    public void ModifyState(bool canCombine, bool canUnlock)
    {
        if (IsLocked)
        {
            CanCombine = false;
            CanUnlock = canUnlock;
        }
        else
        {
            CanUnlock = true;
            CanCombine = canCombine;
        }
    }

    public void TryUnlock()
    {
        if (CanUnlock)
        {
            IsLocked = false;
        }
    }

    public void MakeCombined(int combineOrder)
    {
        CombineOrder = combineOrder;
        SequencerAudio.IsCombined = true;
    }

    public void Reset()
    {
        CombineOrder = 0;
        CanCombine = false;
        CanUnlock = false;
        SequencerAudio.IsCombined = false;
        IsLocked = SequencerAudio.Audio.IsInitialyLocked;
    }

    public void TryMakeCombinable()
    {
        Reset();
        ModifyState(canUnlock: true, canCombine: true);
    }

    public void Renumerate(int newNumber)
    {
        Number = newNumber.ToString();
        Name = $"{SequencerAudio.Audio.Name} {Number}";;
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        Sequencer
            .WhenAnyValue(sequencer => sequencer.State)
            .Subscribe(state => IsAvailable = state is SequencerState.Loaded)
            .ToDisposables(Disposables);
    }
}