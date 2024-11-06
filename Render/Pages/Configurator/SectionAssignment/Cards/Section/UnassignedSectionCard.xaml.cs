using ReactiveUI;
using Render.Kernel.DragAndDrop;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Section
{
    public partial class UnassignedSectionCard
    {
        public UnassignedSectionCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.SectionTitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Number, v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ScriptureReference, v => v.SectionReferenceLabel.Text));
            });
        }

        private void UnassignedSectionDragStarting(object sender, DragAndDropEventArgs args)
        {
            args.Data = ViewModel;
        }
    }
}