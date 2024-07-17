using System.Windows.Input;
using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Models;

public class ToolbarItemModel
{
    public ToolbarItemType Type { get; private set; }

    public int Priority { get; set; }

    public string Icon { get; set; }

    public ICommand ActionCommand { get; set; }

    public string AutomationId { get; set; }

    public ToolbarItemModel(
        ToolbarItemType type,
        string icon,
        ICommand actionCommand,
        string automationId = "")
    {
        Type = type;
        Icon = icon;
        ActionCommand = actionCommand;
        AutomationId = automationId.Equals(string.Empty) ? icon : automationId;
    }
}
