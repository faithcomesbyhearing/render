using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Player;

internal class PlayToolbarItemViewModel : BaseToolbarItemViewModel, IPlayToolbarItem
{
    protected InternalSequencer Sequencer;

    internal PlayToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "RecorderPlay";
        AutomationId = "PlayButton";
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.IsNotPlaying())
        {
            Sequencer.InternalPlayer.Play();
        }

        return base.ActionExecute();
    }
}