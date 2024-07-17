using Render.Sequencer.Contracts.Models;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems;

internal class CustomToolbarItemViewModel : BaseToolbarItemViewModel
{
    internal CustomToolbarItemViewModel(ToolbarItemModel item)
    {
        IconKey = item.Icon;
        ActionCommand = item.ActionCommand;
        AutomationId = item.AutomationId;
    }
}
