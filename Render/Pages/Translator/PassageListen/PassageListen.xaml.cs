using ReactiveUI;

namespace Render.Pages.Translator.PassageListen;

public partial class PassageListen 
{
	public PassageListen()
	{
		InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ReferenceList, v => v.References.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.SupplementaryMaterialList, v => v.SupplementalMaterials.ItemsSource));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading,
                v => v.LoadingView.IsVisible));
            d(this.WhenAnyValue(page => page.ViewModel.SupplementaryMaterialList)
                .Subscribe(list =>
                {
                    SupplementaryMaterialContainer.IsVisible = list?.Any() == true;
                }));
        });
    }
    
}