using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Components.Modal.ModalComponents;

public class SnapshotSelectComponentViewModel : ViewModelBase
{
    public Section Section { get; private set; }
    public readonly ReactiveCommand<bool, Unit> SelectSnapshotCommand;
    public (IUser, Snapshot, Team) Pair { get; private set; }
    public (IUser, Snapshot, Team) OtherPair { get; private set; }
    
    [Reactive] public (IUser User, Snapshot Snapshot, Team Team) SelectedPair { get; private set; }
    [Reactive] public string Username { get; set; }
    [Reactive] public string StageName { get; set; }
    [Reactive] public string ModalBodyText { get; set; }
    public string PairSnapshotDate { get; }
    public string OtherPairSnapshotDate { get; }
    public string PairTeamNumber { get; }
    public string OtherTeamNumber { get; }

    public SnapshotSelectComponentViewModel(
        List<(IUser, Snapshot, Team)> pairs,
        Section section,
        string stageName,
        IViewModelContextProvider viewModelContextProvider) :
        base("SnapshotSelectModal", viewModelContextProvider)
    {
        Pair = pairs[0];
        var teamNumber = pairs[0].Item3?.TeamNumber ?? 1;
        PairSnapshotDate = Pair.Item2.DateUpdated.DateTime.ToString("MMM dd, yyyy, h:mm tt");
        PairTeamNumber = string.Format(AppResources.TeamTitle, teamNumber);
        OtherPair = pairs[1];
        OtherPairSnapshotDate = OtherPair.Item2.DateUpdated.ToString("MMM dd, yyyy, h:mm tt");
        var otherTeamNumber = pairs[1].Item3?.TeamNumber ?? 2;
        OtherTeamNumber = string.Format(AppResources.TeamTitle, otherTeamNumber);
        ModalBodyText = string.Format(AppResources.WhichSnapshot, stageName);
        Section = section;
        StageName = stageName;
        SelectSnapshotCommand = ReactiveCommand.Create<bool>(SelectSnapshot);
    }

    private void SelectSnapshot(bool firstSelected)
    {
        SelectedPair = firstSelected ? Pair : OtherPair;
        Username = SelectedPair.Item1.Username;
        ViewModelContextProvider.GetModalService().Close(DialogResult.Ok);
    }
    
    public override void Dispose()
    {
        Section = null;
        Pair = default;
        OtherPair = default;
        SelectedPair = default;
        SelectSnapshotCommand?.Dispose();
        base.Dispose();
    }
}