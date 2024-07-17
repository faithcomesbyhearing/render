using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.DragAndDrop;
using FlowDirection = Microsoft.Maui.FlowDirection;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public partial class NewSectionViewTeamCard
    {
        public NewSectionViewTeamCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Name,
                    v => v.TeamNameLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.CountString,
                    v => v.CountLabel.Text));
                d(this.WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
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
                    }));
            });
        }

        private void DragGestureRecognizerEffects_OnDragStarting(object sender, DragAndDropEventArgs args)
        {
            args.Data = ViewModel;
        }
    }
}