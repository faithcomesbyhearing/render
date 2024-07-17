using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Player;

internal class PauseToolbarItemViewModel : BaseToolbarItemViewModel, IPlayToolbarItem
{
    protected InternalSequencer Sequencer;

    internal PauseToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "RecorderPause";
        AutomationId = "PauseButton";
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.IsPlaying())
        {
            Sequencer.InternalPlayer.Pause();
        }

        return base.ActionExecute();
    }
}