using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.DragAndDrop;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Team
{
    public partial class TeamCard
    {
        public TeamCard()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.TeamNameLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.AssignedSections.Count, v => v.CountLabel.Text));
                d(this
                    .WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(UpdateTeamCountFrame));
            });
        }

        private void UpdateTeamCountFrame(FlowDirection flowDirection)
        {
            if (flowDirection == FlowDirection.LeftToRight)
            {
                TeamCountFrame.SetValue(TranslationXProperty, 35);
                TeamCountFrame.Margin = new Thickness(0, 0, 40, 0);
            }
            else
            {
                TeamCountFrame.SetValue(TranslationXProperty, -35);
                TeamCountFrame.Margin = new Thickness(0, 0, -30, 0);
            }
        }

        private void DragStarting(object sender, DragAndDropEventArgs args)
        {
            args.Data = ViewModel;
        }
    }
}