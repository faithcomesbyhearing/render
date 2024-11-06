using System.Collections.ObjectModel;
using ReactiveUI;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;

namespace Render.Pages.Configurator.SectionAssignment.Tabs.Team
{
    public partial class TeamViewTab
    {
        public TeamViewTab()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Manager.UnassignedSectionCards, v => v.SectionCollection.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm, v => v.TeamAssignments.BindingContext));
                d(this.WhenAnyValue(tab => tab.ViewModel.Manager.TeamCards).Subscribe(SetItemSource));
            });
        }

        private void SetItemSource(ObservableCollection<TeamCardViewModel> cards)
        {
            var source = BindableLayout.GetItemsSource(TeamCollection);
            if (source == null)
            {
                BindableLayout.SetItemsSource(TeamCollection, cards);
            }
        }

        protected override void Dispose(bool disposing)
        {
            SectionCollection.ItemsSource = null;
            SectionCollection.ClearLogicalChildren();

            TeamCollection.ClearLogicalChildren();
            TeamAssignments.Dispose();

            base.Dispose(disposing);
        }
    }
}