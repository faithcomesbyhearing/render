using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems;

internal static class ToolbarItemViewModelExtensions
{
    public static void SetState(this BaseToolbarItemViewModel? button, bool active)
    {
        if (button is not null)
        {
            button.State = active ? ToolbarItemState.Active : ToolbarItemState.Disabled;
        }
    }

    public static void SetIsAvailable(this BaseToolbarItemViewModel? button, bool isAvailable)
    {
        if (button is not null)
        {
            button.IsAvailable = isAvailable;
        }
    }
}