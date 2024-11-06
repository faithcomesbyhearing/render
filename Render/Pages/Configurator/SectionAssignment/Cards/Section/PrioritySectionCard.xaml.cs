using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.DragAndDrop;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Section
{
    public partial class PrioritySectionCard
    {
        public PrioritySectionCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Number, v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.SectionTitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ScriptureReference, v => v.SectionReferenceLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTeam, v => v.UserStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTeam, v => v.DropRectangle.IsVisible, showTeam => !showTeam));
                d(this.OneWayBind(ViewModel, vm => vm.AssignedTeamViewModel.Name, v => v.UserNameLabel.Text));
                d(this.BindCommand(ViewModel, vm => vm.RemoveTeamCommand, v => v.RemoveButtonGestureRecognizer, nameof(RemoveButtonGestureRecognizer.Tapped)));
                d(Observable
                    .FromEventPattern<DragAndDropEventArgs>(TeamDropRecognizerEffect, nameof(TeamDropRecognizerEffect.Drop))
                    .Select(pattern => pattern.EventArgs?.Data is TeamCardViewModel team ? team : null)
                    .InvokeCommand(this, v => v.ViewModel.AssignTeamCommand));
            });
        }
    }
}