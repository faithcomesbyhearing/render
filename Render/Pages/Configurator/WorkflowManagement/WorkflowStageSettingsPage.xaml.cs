using ReactiveUI;
using Render.Components.StageSettings;
using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Components.StageSettings.ConsultantCheckStageSettings;
using Render.Components.StageSettings.DraftingStageSettings;
using Render.Components.StageSettings.PeerCheckStageSettings;

namespace Render.Pages.Configurator.WorkflowManagement;

//implement after https://dev.azure.com/FCBH/Software%20Development/_workitems/edit/11779
public partial class WorkflowStageSettingsPage
{
    public WorkflowStageSettingsPage()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ProceedButtonViewModel, v => v.ProceedButton.BindingContext));
            d(this.WhenAnyValue(x => x.ViewModel.CurrentStageSettingsViewModel)
                .Subscribe(BuildPage));
            d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
        });
    }

    private void BuildPage(StageSettingsViewModelBase viewModel)
    {
        // MainStack.Children.Add(new Button()
        // {
        //     WidthRequest = 50,
        //     HeightRequest = 50,
        //     BackgroundColor = Colors.Red
        // });
        
        // var pieceToRemove = (View)LogicalChildren[0].LogicalChildren.FirstOrDefault();
        // if (pieceToRemove != null)
        // {
        //     MainStack.Children.Remove(pieceToRemove);
        // }
        
        IView pieceToAdd;
        switch (viewModel)
        {
            case DraftingStageSettingsViewModel draftingStageSettingsViewModel:
                pieceToAdd = new DraftingStageSettings()
           { ViewModel = draftingStageSettingsViewModel };
                break;
            case PeerCheckStageSettingsViewModel peerCheckStageSettingsViewModel:
                pieceToAdd = new PeerCheckStageSettings()
                { ViewModel = peerCheckStageSettingsViewModel };
                break;
            case CommunityTestStageSettingsViewModel communityCheckStageSettingsViewModel:
                pieceToAdd = new CommunityTestStageSettings()
                    { ViewModel = communityCheckStageSettingsViewModel };
                break;
            case ConsultantCheckStageSettingsViewModel consultantCheckStageSettingsViewModel:
                pieceToAdd = new ConsultantCheckStageSettings()
                    { ViewModel = consultantCheckStageSettingsViewModel };
                break;
            default:
                return;
        }
        
        MainStack.Children.Add(pieceToAdd);
    }

    // protected override void OnDisappearing()
    // {
    //     if (ViewModel != null && ViewModel.CurrentStageSettingsViewModel != null)
    //     {
    //         ViewModel.CurrentStageSettingsViewModel = null;
    //     }
    //     base.OnDisappearing();
    // }
}