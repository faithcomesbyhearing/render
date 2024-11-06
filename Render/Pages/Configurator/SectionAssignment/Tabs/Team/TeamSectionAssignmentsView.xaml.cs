using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.DragAndDrop;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;

namespace Render.Pages.Configurator.SectionAssignment.Tabs.Team
{
    public partial class TeamSectionAssignmentsView
    {
        public TeamSectionAssignmentsView()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.SelectedTeam.AssignedSections, v => v.AssignedSectionCollection.ItemsSource));
                d(this.BindCommand(ViewModel, vm => vm.SectionIndexChangedCommand, v => v.AssignedSectionCollection, nameof(AssignedSectionCollection.ReorderCompleted)));
                d(this.WhenAnyValue(v => v.ViewModel.Manager.UnassignedSectionCards.Count).Subscribe(ChangeDropZoneVisibility));
                d(Observable
                    .FromEventPattern<DragAndDropEventArgs>(SectionDropEffect, nameof(SectionDropEffect.Drop))
                    .Select(pattern => pattern.EventArgs?.Data is SectionCardViewModel team ? team : null)
                    .InvokeCommand(this, v => v.ViewModel.AssignSectionCommand));
            });
        }

        private void ChangeDropZoneVisibility(int unassignedSectionCount)
        {
            if (unassignedSectionCount is not 0 && dropZoneRow.Height.Value is not 0)
            {
                return;
            }

            dropZoneRow.Height = new GridLength(unassignedSectionCount is 0 ? 0 : 110);
        }

        protected override void Dispose(bool disposing)
        {
            AssignedSectionCollection.ItemsSource = null;
            AssignedSectionCollection.ClearLogicalChildren();

            base.Dispose(disposing);
        }
    }
}