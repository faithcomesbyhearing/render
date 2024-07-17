using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;

internal class StopToolbarItemViewModel : BaseToolbarItemViewModel, IRecordToolbarItem
{
    protected InternalSequencer Sequencer;

    internal StopToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "RecorderStop";
        AutomationId = "RecorderStopButton";
        Option = ItemOption.Required;
    }

    protected override Task ActionExecute()
    {
        if (Sequencer.IsRecording())
        {
            return Sequencer.InternalRecorder.StopAsync();
        }

        return base.ActionExecute();
    }
}