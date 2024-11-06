using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Pages.AppStart.ProjectSelect;

public partial class ProjectSelect
{
    public ProjectSelect()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProjectListViewModel, v => v.ProjectListTab.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.AddProjectViewModel, v => v.AddProjectTab.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.IsRenderUser,
                v => v.PageSelectorContainer.IsVisible, VisibilitySelector));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectedAddProject, v => v.SelectAddNewProjectViewTap));
            d(this.BindCommand(ViewModel, vm => vm.OnSelectedProjectList, v => v.SelectProjectListViewTap));
            d(this.OneWayBind(ViewModel, vm => vm.ShowProjectListPanel,
                v => v.ProjectListTab.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.ShowProjectListPanel,
                v => v.AddProjectTab.IsVisible, VisibilitySelector));
            d(this.OneWayBind(ViewModel, vm => vm.ShowProjectListPanel,
                v => v.AddProjectFromComputer.IsVisible, VisibilitySelector));
            d(this.OneWayBind(ViewModel, vm => vm.ShowProjectListPanel,
                v => v.ShowOffloadSwitch.IsVisible));
            d(this.WhenAnyValue(x => x.ViewModel.ShowProjectListPanel)
                .Subscribe(ChangeLabelColors));
            d(this.Bind(ViewModel, vm => vm.ProjectListViewModel.OffloadMode, v => v.OffloadSwitch.IsToggled));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading,
                v => v.LoadingView.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.AddProjectViewModel.AddFromComputerViewModel,
                v => v.AddViaFolderView.BindingContext));
            d(this.BindCommandCustom(AddAProjectFromComputerGesture, v => v.ViewModel.AddProjectFromComputerCommand));
            d(this.WhenAnyValue(x => x.ViewModel.AddProjectViewModel.AddFromComputerViewModel.ShowProgressView)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ViewSelector));
            
#if DEMO
            AddProjectFromComputer.HeightRequest = 0;
#endif
        });
    }

    private void ChangeLabelColors(bool showProjectList)
    {
        if (showProjectList)
        {
            SetButtonActive(ProjectListViewButton);
            SetButtonInactive(AddProjectViewButton);
        }
        else
        {
            SetButtonActive(AddProjectViewButton);
            SetButtonInactive(ProjectListViewButton);
        }
    }

    private static void SetButtonActive(View labelButton)
    {
        labelButton.SetValue(BackgroundColorProperty, ResourceExtensions.GetColor("Option"));
        labelButton.SetValue(Label.TextColorProperty, ResourceExtensions.GetColor("SecondaryText"));
    }

    private static void SetButtonInactive(View labelButton)
    {
        labelButton.SetValue(BackgroundColorProperty, ResourceExtensions.GetColor("AlternateBackground"));
        labelButton.SetValue(Label.TextColorProperty, ResourceExtensions.GetColor("Option"));
    }
    
    private void ViewSelector(bool showAddFromComputer)
    {
        if (!showAddFromComputer)
        {
            ProjectSelectView.SetValue(IsVisibleProperty, true);
            AddViaFolderView.SetValue(IsVisibleProperty, false);
        }
        else
        {
            ProjectSelectView.SetValue(IsVisibleProperty, false);
            AddViaFolderView.SetValue(IsVisibleProperty, true);
        }
    }

    private static bool VisibilitySelector(bool arg)
    {
        return !arg;
    }
}