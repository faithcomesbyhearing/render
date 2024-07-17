using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Flag;

internal class FlagToolbarItemViewModel : BaseToolbarItemViewModel, IFlagToolbarItem
{
    protected InternalSequencer Sequencer;

    internal FlagToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = sequencer.FlagType is FlagType.Note ? "Union" : "AddFlag";
        AutomationId = "FlagButton";
    }

    protected override Task ActionExecute()
    {
        return Sequencer.AddFlag();
    }
}