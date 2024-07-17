using ReactiveUI;

namespace Render.Pages.Translator.DraftingPage
{
    public partial class Drafting
    {
        public Drafting()
        {
            InitializeComponent();
                
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModels, v => v.References.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.DraftViewModels, v => v.Drafts.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.SequencerRecorderViewModel, v => v.Sequencer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
                
                /*
                // TODO fix binding.
                // The following Rx Binding currently does not work:
                //      d(this.Bind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
                //
                // Workaround:
                d(ViewModel.ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                   .Subscribe(async isExecuting =>
                   {
                       if (isExecuting)
                       {
                           LoadingView.IsVisible = true;
                           await Task.Delay(1000); // simulate a long task
                           LoadingView.IsVisible = false;
                       }
                   }));
                 */
                
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
            });
        }       

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}