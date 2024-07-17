using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Flags.Base;

public class BaseFlagViewModel : BaseViewModel, IFlag
{
    /// <summary>
    /// Key to identify specific flag with regards to client domain models
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// SequencerAudio hash code to identify flag view model in WaveFormItemViewModel
    /// </summary>
    public int AudioHashCode { get; set; }

    /// <summary>
    /// String representation of icon's x:Key 
    /// attribute name in general XAML resources
    /// </summary>
    [Reactive]
    public string? IconKey { get; set; }

    /// <summary>
    /// Ratio to convert position in seconds to X coordinate for WaveFormItemViewModel
    /// </summary>
    [Reactive]
    public double SecToDipPositionRatio { get; set; }

    /// <summary>
    /// Ratio to convert position in seconds to X coordinate for MiniWaveFormItemViewModel in Scroller
    /// </summary>
    [Reactive]
    public double MiniSecToDipPositionRatio { get; set; }

    /// <summary>
    /// Flag position in seconds with regard to audio duration
    /// </summary>
    [Reactive]
    public required double PositionSec { get; init; }

    /// <summary>
    /// Absolute flag position in seconds with regard to all total audio duration
    /// </summary>
    [Reactive]
    public required double AbsPositionSec { get; init; }

    /// <summary>
    /// Flag X coordinate in device independent pixels with regard to waveform width
    /// </summary>
    [Reactive]
    public double PositionDip { get; protected set; }

    /// <summary>
    /// Flag X coordinate in device independent pixels with regard to mini waveform item width
    /// </summary>
    [Reactive]
    public double MiniPositionDip { get; protected set; }

    /// <summary>
    /// Controls combined visual look based on State and Option
    /// </summary>
    [Reactive]
    public FlagVisualState VisualState { get; set; }

    /// <summary>
    /// Controls visual look for unread, read, disabled states
    /// </summary>
    [Reactive]
    public FlagState State { get; set; }

    /// <summary>
    /// Controls visual look for optional, required options
    /// </summary>
    [Reactive]
    public ItemOption Option { get; set; }

    [Reactive]
    public ICommand? TapCommand { get; set; }

    public BaseFlagViewModel()
    {
        SetupListeners();
    }

    private void SetupListeners()
    {
        this
            .WhenAnyValue(flag => flag.SecToDipPositionRatio)
            .Where(ratio => ratio > 0)
            .Subscribe(ratio => PositionDip = PositionSec / ratio)
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(flag => flag.MiniSecToDipPositionRatio)
            .Where(ratio => ratio > 0)
            .Subscribe(ratio => MiniPositionDip = PositionSec / ratio)
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(item => item.State)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe((value) => VisualState = GetVisualState(value, Option))
            .ToDisposables(Disposables);
        
        this
            .WhenAnyValue(item => item.Option)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe((value) => VisualState = GetVisualState(State, value))
            .ToDisposables(Disposables);
    }

    /// <summary>
    /// Determines flag's visual state base on state and option
    /// </summary>
    protected virtual FlagVisualState GetVisualState(FlagState state, ItemOption option)
    {
        return state switch
        {
            FlagState.Unread when option is ItemOption.Optional => FlagVisualState.OptionalUnread,
            FlagState.Unread when option is ItemOption.Required => FlagVisualState.RequiredUnread,
            FlagState.Disabled => FlagVisualState.Disabled,
            FlagState.Read => FlagVisualState.Read,
            _ => FlagVisualState.Read,
        };
    }
}