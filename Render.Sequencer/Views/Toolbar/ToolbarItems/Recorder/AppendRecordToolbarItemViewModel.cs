using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.ToolbarItems;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems.Recorder;

internal class AppendRecordToolbarItemViewModel : BaseToolbarItemViewModel, IAppendRecordToolbarItem
{
    protected InternalSequencer Sequencer;

    [Reactive]
    public string AppendRecordIcon { get; set; } = "RecorderAppend";

    internal AppendRecordToolbarItemViewModel(InternalSequencer sequencer)
    {
        Sequencer = sequencer;

        IconKey = AppendRecordIcon;
        AutomationId = "AppendRecordButton";

        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.AppendRecordMode,
                sequencer => sequencer.State,
                sequencer => sequencer.CurrentAudio.IsTemp)
            .Subscribe(((bool AppendRecord, SequencerState State, bool _) options) =>
            {
                State =
                    options.AppendRecord ||
                    Sequencer.CurrentAudio.IsTempOrEmpty ||
                    options.State is not SequencerState.Loaded ?
                        ToolbarItemState.Disabled :
                        ToolbarItemState.Active;
            })
            .ToDisposables(Disposables);
    }

    protected override Task ActionExecute()
    {
        AppendRecord();

        return base.ActionExecute();
    }

    private void AppendRecord()
    {
        Sequencer.Mode = SequencerMode.Recorder;
        Sequencer.AppendRecordMode = true;
    }
}