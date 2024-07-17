using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Utils.Extensions;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems;

public abstract class BaseToolbarItemViewModel : BaseViewModel, IToolbarItem
{
    /// <summary>
    /// Changes item visibility
    /// </summary>
    [Reactive]
    public bool IsAvailable { get; set; }

    /// <summary>
    /// String representation of icon's x:Key 
    /// attribute name in general XAML resources
    /// </summary>
    [Reactive]
    public string? IconKey { get; set; }

    /// <summary>
    /// Controls visual look for active, disabled, toggled states
    /// </summary>
    [Reactive]
    public ToolbarItemState State { get; set; }

    /// <summary>
    /// Controls visual look for optional, required options
    /// </summary>
    [Reactive]
    public ItemOption Option { get; set; }

    /// <summary>
    /// Controls combined visual look based on State and Option
    /// </summary>
    [Reactive]
    public ToolbarItemVisualState VisualState { get; set; }

    [Reactive]
    public ICommand? ActionCommand { get; set; }

    [Reactive]
    public string AutomationId { get; set; }

    public BaseToolbarItemViewModel()
    {
        IsAvailable = true;
        AutomationId = string.Empty;
        ActionCommand = ReactiveCommand.CreateFromTask(
            execute: ActionExecute,
            canExecute: this
                .WhenAnyValue(item => item.State)
                .Select(state => state is not ToolbarItemState.Disabled)
                .ObserveOn(RxApp.MainThreadScheduler));

        SetupListeners();
    }

    private void SetupListeners()
    {
        this
            .WhenAnyValue(item => item.State, item => item.Option)
            .Subscribe((values) => VisualState = GetVisualState(values.Item1, values.Item2))
            .ToDisposables(Disposables);
    }

    /// <summary>
    /// Determines toolbar item's visual state base on state and option
    /// </summary>
    protected virtual ToolbarItemVisualState GetVisualState(ToolbarItemState state, ItemOption option)
    {
        return state switch
        {
            ToolbarItemState.Active when option is ItemOption.Optional => ToolbarItemVisualState.ActiveOptional,
            ToolbarItemState.Active when option is ItemOption.Required => ToolbarItemVisualState.ActiveRequired,
            ToolbarItemState.Disabled => ToolbarItemVisualState.Disabled,
            ToolbarItemState.Toggled when option is ItemOption.Optional => ToolbarItemVisualState.ToggledOptional,
            ToolbarItemState.Toggled when option is ItemOption.Required => ToolbarItemVisualState.ToggledRequired,
            _ => ToolbarItemVisualState.ActiveOptional,
        };
    }

    protected virtual Task ActionExecute() 
    {
        return Task.CompletedTask;
    }
}