using Render.Sequencer.Contracts.Enums;
using System.Windows.Input;

namespace Render.Sequencer.Contracts.ToolbarItems;

public interface IToolbarItem : IDisposable
{
    public bool IsAvailable { get; set; }

    /// <summary>
    /// String representation of icon's x:Key 
    /// attribute name in general XAML resources
    /// </summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Controls visual look for active, disabled, toggled states
    /// </summary>
    public ToolbarItemState State { get; set; }

    /// <summary>
    /// Controls visual look for optional, required options
    /// </summary>
    public ItemOption Option { get; set; }

    public ICommand? ActionCommand { get; set; }
}