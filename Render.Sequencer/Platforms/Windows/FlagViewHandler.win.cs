using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Render.Sequencer.Platforms.Windows.Models;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Platforms;

public class FlagViewHandler : ContentViewHandler
{
    protected override void ConnectHandler(ContentPanel platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView is BaseFlagView noteFlagView)
        {
            platformView.Tag = new ViewTag(noteFlagView.ViewTag, NoteFlagTapped);
        }
    }

    protected override void DisconnectHandler(ContentPanel platformView)
    {
        platformView.Tag = null;

        base.DisconnectHandler(platformView);
    }

    private void NoteFlagTapped()
    {
        if (VirtualView is BaseFlagView flagView)
        {
            flagView.SendTapped();
        }
    }
}