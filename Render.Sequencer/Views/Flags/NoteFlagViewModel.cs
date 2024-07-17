using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Views.Flags;

public class NoteFlagViewModel : BaseFlagViewModel
{
    /// <summary>
    /// Controls flag direction to avoid collisions between nearby flags
    /// </summary>
    [Reactive]
    public FlagDirection Direction { get; set; }

    public NoteFlagViewModel()
    {
        IconKey = "RecordANoteOrSuggestRevision";
    }
}