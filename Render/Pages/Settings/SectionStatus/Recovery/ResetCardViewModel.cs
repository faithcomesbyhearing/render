using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Pages.Settings.SectionStatus.Recovery;

public class ResetCardViewModel : ViewModelBase
{
    private Func<Guid, Task> _resetSectionCallback;

    private Section Section { get; set; }

    [Reactive] public bool SectionHasSnapshots { get; set; }
    
    public ReactiveCommand<Unit, Unit> ResetSectionCommand;

    public ResetCardViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Section section,
        Func<Guid, Task> sectionResetCallback, bool sectionHasSnapshots = false) :
        base("SnapshotCard", viewModelContextProvider)
    {
        _resetSectionCallback = sectionResetCallback;
        Section = section;
        SectionHasSnapshots = sectionHasSnapshots;
        ResetSectionCommand = ReactiveCommand.Create(ResetSection);
    }

    private void ResetSection()
    {
        _resetSectionCallback?.Invoke(Guid.Empty);
    }

    public override void Dispose()
    {
        _resetSectionCallback = null;

        Section = null;

        ResetSectionCommand?.Dispose();
        ResetSectionCommand = null;

        base.Dispose();
    }
}