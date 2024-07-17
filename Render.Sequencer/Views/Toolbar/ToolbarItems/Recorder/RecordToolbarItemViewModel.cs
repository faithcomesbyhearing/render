using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;

internal class RecordToolbarItemViewModel : BaseToolbarItemViewModel, IRecordToolbarItem
{
    protected InternalSequencer Sequencer;

    internal RecordToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "RecorderRecord";
        AutomationId = "RecorderRecordButton";
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.IsNotRecording())
        {
            return Sequencer.InternalRecorder.StartAsync();
        }

        return base.ActionExecute();
    }
}
