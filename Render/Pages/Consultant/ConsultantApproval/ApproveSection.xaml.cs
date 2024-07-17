using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public partial class ApproveSection 
    {
        public ApproveSection()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.SequencerPlayerViewModel, v => v.Sequencer.BindingContext));
                d(this.BindCommand(ViewModel, vm => vm.ApproveCommand, v => v.ApproveProceedButtonGestureRecognizer));
                d(this.BindCommand(ViewModel, vm => vm.ReturnCommand, v => v.ReturnButtonGestureRecognizer));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
                d(this
                    .WhenAnyValue(x => x.ViewModel.BackTranslatePlayers)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(BackTranslatePlayers);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(BackTranslatePlayers, x);
                        }
                    }));
                d(this
                    .WhenAnyValue(x => x.ViewModel.FlowDirection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(flowDirection =>
                    {
                        if (flowDirection == FlowDirection.LeftToRight)
                        {
                            ApproveProceedButton.Margin = new Thickness(0,0,-5,-5);
                            ReturnButton.Margin = new Thickness(-5,-5,0,-3);
                        }
                        else
                        {
                            ApproveProceedButton.Margin = new Thickness(-3,0,-5,-5);
                            ReturnButton.Margin = new Thickness(-7,-5,-3,-3);
                        }
                    }));
            });
        }

        protected override void OnDisappearing()
        {
            if (ViewModel != null)
            {
                ViewModel.PauseSectionTitlePlayer();
                ViewModel.SequencerPlayerViewModel.StopCommand.Execute().Subscribe();

                foreach (var players in ViewModel.BackTranslatePlayers)
                {
                    players.Pause();
                }
            }
        }
    }
}