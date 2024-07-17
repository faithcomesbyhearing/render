using ReactiveUI;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Editor;

internal class EditorAppendRecordToolbarItemViewModel : BaseToolbarItemViewModel, IToolbarItem
{
    protected InternalSequencer Sequencer;

    internal EditorAppendRecordToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "RecorderAppend";
        ActionCommand = ReactiveCommand.CreateFromTask(EditorAppendRecordAsync);
        AutomationId = "AppendRecordButton";
    }

    private async Task EditorAppendRecordAsync()
    {
        await Sequencer.InsertAudioAsync();
    }
}
