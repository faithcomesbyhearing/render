using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Components.Modal.ModalComponents;

public class SelectedSnapshotConfirmationMessageViewModel : ViewModelBase
{
    public string BodyMessage { get; private set; }
    public string TeamNumber { get; }
    public string SnapshotUpdatedData { get; }
    
    public SelectedSnapshotConfirmationMessageViewModel(
        (IUser, Snapshot, Team) selectedPair,
        Section section,
        IViewModelContextProvider viewModelContextProvider) :
        base("SnapshotConfirmationMessage", viewModelContextProvider)
    {
        var sectionNumber = string.Format(AppResources.Section, section.Number);
        TeamNumber = string.Format(AppResources.TeamTitle, selectedPair.Item3?.TeamNumber ?? 2);
        SnapshotUpdatedData = selectedPair.Item2.DateUpdated.DateTime.ToString("MMM dd, yyyy, h:mm tt");
        
        BodyMessage = string.Format(AppResources.SelectedSnapshotMessageTemplate, sectionNumber, section.ScriptureReference, selectedPair.Item1.Username,
            TeamNumber, SnapshotUpdatedData);
    }
}