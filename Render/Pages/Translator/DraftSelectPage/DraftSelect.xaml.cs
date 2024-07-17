using ReactiveUI;

namespace Render.Pages.Translator.DraftSelectPage
{
    public partial class DraftSelect
    {
        public DraftSelect()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.Drafts, v => v.DraftList.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}