using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Combine;

internal class UndoCombineToolbarItemViewModel : BaseToolbarItemViewModel
{
    protected InternalSequencer Sequencer;

    internal UndoCombineToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "Undo";
        AutomationId = "UndoCombine";
        State = ToolbarItemState.Disabled;
    }

    protected override Task ActionExecute()
    {
        Sequencer.UndoLastCombinedAudio();

        return base.ActionExecute();
    }
}