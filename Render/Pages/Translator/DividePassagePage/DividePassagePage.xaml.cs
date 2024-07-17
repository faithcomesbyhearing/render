using ReactiveUI;
using Render.Components.DivisionPlayer;
using Render.Extensions;

namespace Render.Pages.Translator.DividePassagePage
{
    public partial class DividePassagePage
    {
        public DividePassagePage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
                d(this.WhenAnyValue(x => x.ViewModel.DivisionPlayers)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(DivisionPlayerStackLayout);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(DivisionPlayerStackLayout, x);
                        }
                    }));
            });
        }
        
        protected override void Dispose(bool disposing)
        {
            DivisionPlayerStackLayout
                .Cast<DivisionPlayer>()
                .ForEach(player => player.Dispose());

            base.Dispose(disposing);
        }
    }
}