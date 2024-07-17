using ReactiveUI;

namespace Render.Components.Modal.ModalComponents;

public partial class SnapshotSelectComponentView 
{
    public SnapshotSelectComponentView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.SelectStack.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.StageName, 
                    v => v.StageName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.PairTeamNumber, 
                    v => v.TeamLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Pair.Item1.FullName, 
                    v => v.TeamName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.PairSnapshotDate, 
                    v => v.SnapshotDate.Text));
                d(this.OneWayBind(ViewModel, vm => vm.OtherTeamNumber, 
                    v => v.OtherTeamLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.OtherPair.Item1.FullName, 
                    v => v.OtherTeamName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.OtherPairSnapshotDate, 
                    v => v.OtherTeamSnapshotDate.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ModalBodyText, 
                    v => v.BodyText.Text));
            });
    }
    
    private void OnTeamOneClicked(object sender, EventArgs e)
    {
        ViewModel?.SelectSnapshotCommand.Execute(true).Subscribe();
    }
        
    private void OnTeamTwoClicked(object sender, EventArgs e)
    {
        ViewModel?.SelectSnapshotCommand.Execute(false).Subscribe();
    }
}