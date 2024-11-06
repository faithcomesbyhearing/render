using ReactiveUI;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Section
{
    public partial class TeamSectionCard
    {
        public TeamSectionCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Number, v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.SectionTitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ScriptureReference, v => v.SectionReferenceLabel.Text));
                d(this.BindCommand(ViewModel, vm => vm.RemoveTeamCommand, v => v.RemoveButtonGestureRecognizer, nameof(RemoveButtonGestureRecognizer.Tapped)));
            });
        }
    }
}