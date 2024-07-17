﻿using ReactiveUI;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Editor;

internal class EditorUndoToolbarItemViewModel : BaseToolbarItemViewModel, IToolbarItem
{
    protected InternalSequencer Sequencer;

    internal EditorUndoToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = "DeleteUndo";
        ActionCommand = ReactiveCommand.CreateFromTask(UndoDeleteAsync);
        AutomationId = "UndoDeleteButton";
    }

    private async Task UndoDeleteAsync()
    {
        Sequencer.UndoDeleteAudio();
    }
}