using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;

internal class UndoDeleteToolbarItemViewModel : BaseToolbarItemViewModel, IDeleteToolbarItem
{
    protected InternalSequencer Sequencer;

    internal UndoDeleteToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "DeleteUndo";
        AutomationId = "UndoDeleteButton";
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.CurrentAudio.IsTemp)
        {
            return Sequencer.InternalRecorder.RevertTempRecordAsync();
        }

        return base.ActionExecute();
    }
}