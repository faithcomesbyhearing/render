using ReactiveUI;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public partial class TeamViewTeamAssignments 
    {
        public TeamViewTeamAssignments()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.SectionAssignments, 
                    v => v.AssignedSectionCollection.ItemsSource));
            });
        }
    }
}