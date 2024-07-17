using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Editor;

internal class EditorDeleteToolbarItemViewModel : BaseToolbarItemViewModel, IToolbarItem
{
    protected InternalSequencer Sequencer;

    internal EditorDeleteToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "Delete";
        AutomationId = "DeleteButton";
    }

    protected override Task ActionExecute()
    {
        Sequencer.DeleteAudio();

        return base.ActionExecute();
    }
}