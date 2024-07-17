using ReactiveUI;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Editor;

internal class EditorCutToolbarItemViewModel : BaseToolbarItemViewModel, IToolbarItem
{
    protected InternalSequencer Sequencer;

    internal EditorCutToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "DivisionOrCut";
        ActionCommand = ReactiveCommand.Create(CutAudio);
        AutomationId = "CutButton";
    }

    private void CutAudio()
    {
        Sequencer.CutAudio();
    }
}