using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;

internal class DeleteToolbarItemViewModel : BaseToolbarItemViewModel, IDeleteToolbarItem
{
    protected InternalSequencer Sequencer;

    internal DeleteToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "Delete";
        AutomationId = "DeleteButton";
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.CurrentAudio.IsTemp is false)
        {
            return Sequencer.InternalRecorder.MakeRecordTempAsync();
        }

        return base.ActionExecute();
    }
}