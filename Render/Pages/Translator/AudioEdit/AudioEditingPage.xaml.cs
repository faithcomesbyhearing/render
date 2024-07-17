using ReactiveUI;

namespace Render.Pages.Translator.AudioEdit
{
    public partial class AudioEditingPage
    {
        public AudioEditingPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.SequencerEditorViewModel,
                    v => v.Sequencer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.MiniWaveformPlayerViewModel,
                    v => v.MiniWaveformPlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
            });
        }
    }
}