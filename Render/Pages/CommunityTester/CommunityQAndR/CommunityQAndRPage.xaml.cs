using ReactiveUI;

namespace Render.Pages.CommunityTester.CommunityQAndR
{
    public partial class CommunityQAndRPage
    {
        public CommunityQAndRPage()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel,
                    v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel,
                    v => v.ProceedButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.SequencerRecorderViewModel,
                    v => v.Sequencer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.AudioClipPlayer,
                    v => v.AudioClipPlayer.BindingContext));
				d(this.OneWayBind(ViewModel, vm => vm.ShowQuestionPlayer,
					v => v.QuestionPlayer.IsVisible));
				d(this.OneWayBind(ViewModel, vm => vm.QuestionPlayer,
                    v => v.QuestionPlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.SectionPlayer,
                    v => v.SectionPlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.PassagePlayer,
                    v => v.PassagePlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ShowAudioClipPlayer,
                    v => v.AudioClipPlayer.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading,
                    v => v.LoadingView.IsVisible));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}