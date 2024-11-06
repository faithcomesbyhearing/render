using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.AppStart.Home;

public partial class Home
{
	public Home()
	{
		InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, 
                v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm=> vm.AdministrationNavigationPane, 
                v => v.AdministrationNavigationPane.BindingContext));
            d(this.OneWayBind(ViewModel, vm=> vm.WorkflowNavigationPane, 
                v => v.WorkflowNavigationPane.BindingContext));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectAdminViewCommand, 
                v => v.SelectAdminViewTap));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectWorkflowViewCommand, 
                v => v.SelectWorkflowViewTap));
            d(this.WhenAnyValue(x => x.ViewModel.ShowAdminPanel)
                .Subscribe(ChangeLabelColors));
            d(this.OneWayBind(ViewModel, vm => vm.ShowAdminPanel, 
                v => v.AdministrationNavigationPane.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.ShowAdminPanel, 
                v => v.WorkflowNavigationPane.IsVisible, SetVisibility));
            d(this.OneWayBind(ViewModel, vm => vm.ShowNavigationPanelOptions, 
                v => v.ViewSelectStack.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.IsUserAdmin, 
                v => v.TopMenuBar.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, 
                v => v.LoadingView.IsVisible));
        });
    }
    
    public bool SetVisibility(bool isVisible)
    {
        return !isVisible;
    }
        
    private void ChangeLabelColors(bool showAdminPanel)
    {
        if (showAdminPanel)
        {
            SetButtonActive(AdminViewButton);
            SetButtonInactive(WorkflowViewButton);
        }
        else
        {
            SetButtonActive(WorkflowViewButton);
            SetButtonInactive(AdminViewButton);
        }
    }
        
    private void SetButtonActive(View labelButton)
    {
        var option = (ColorReference)ResourceExtensions.GetResourceValue("Option");
        var textColor = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        labelButton.SetValue(BackgroundColorProperty, option);
        labelButton.SetValue(Label.TextColorProperty, textColor);
    }

    private void SetButtonInactive(View labelButton)
    {
        var offBackground = (ColorReference)ResourceExtensions.GetResourceValue("AlternateBackground");
        var offText = (ColorReference)ResourceExtensions.GetResourceValue("Option");
        labelButton.SetValue(BackgroundColorProperty, offBackground);
        labelButton.SetValue(Label.TextColorProperty, offText);
    }

    protected override void OnAppearing()
    {
        ViewModel?.EntityChangeListenerService.ResetListeners();
    }
    protected override void OnDisappearing()
    {
        ViewModel?.EntityChangeListenerService.RemoveListeners();
    }
        
    protected override bool OnBackButtonPressed()
    {
        ViewModel?.NavigateToProjectSelectPageCommand.Execute();
        return true;
    }
        
    protected override void Dispose(bool disposing)
    {
        TitleBar?.Dispose();
        AdministrationNavigationPane?.Dispose();
        WorkflowNavigationPane?.Dispose();

        base.Dispose(disposing);
    }
}