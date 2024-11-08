using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.ProceedButton;
using Render.Components.StageSettings;
using Render.Components.StageSettings.CommunityTestStageSettings;
using Render.Components.StageSettings.ConsultantApprovalStageSettings;
using Render.Components.StageSettings.ConsultantCheckStageSettings;
using Render.Components.StageSettings.DraftingStageSettings;
using Render.Components.StageSettings.PeerCheckStageSettings;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.WorkflowManagement;

public class WorkflowStageSettingsPageViewModel : PageViewModelBase
{
    [Reactive]
    public StageSettingsViewModelBase CurrentStageSettingsViewModel { get; set; }

    private Action<Stage> _updateStageCard;
    public ProceedButtonViewModel ProceedButtonViewModel { get; private set; }
    private RenderWorkflow Workflow { get; set; }
    private Stage Stage { get; set; }

    public WorkflowStageSettingsPageViewModel(
        Stage stage,
        RenderWorkflow workflow,
        IViewModelContextProvider viewModelContextProvider,
        Action<Stage> updateStageCard,
        string projectName)
        : base(
            urlPathSegment: "WorkflowStageConfiguration",
            viewModelContextProvider: viewModelContextProvider,
            pageName: AppResources.ConfigureWorkflow,
            secondPageName: projectName)
    {
        var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
        if (color != null)
        {
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ConfigureWorkflow, color.Color)?.Glyph;
        }

        _updateStageCard = updateStageCard;
        Stage = stage;
        Workflow = workflow;
        ProceedButtonViewModel = new ProceedButtonViewModel(viewModelContextProvider);
        ProceedButtonViewModel.SetCommand(ProceedWithChanges);
        Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
            .Subscribe(isExecuting => { IsLoading = isExecuting; }));

        Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand
            .ThrownExceptions
            .Subscribe(async exception =>
            {
                await ErrorManager.ShowErrorPopupAsync(viewModelContextProvider, exception);

                await CurrentStageSettingsViewModel.Restore();
            }));

        SetActiveStageToEdit(Stage, Workflow);
    }

    private void SetActiveStageToEdit(Stage stage, RenderWorkflow workflow)
    {
        switch (stage.StageType)
        {
            case StageTypes.Drafting:
                CurrentStageSettingsViewModel = new DraftingStageSettingsViewModel(workflow, stage, ViewModelContextProvider, _updateStageCard);
                break;
            case StageTypes.PeerCheck:
                CurrentStageSettingsViewModel = new PeerCheckStageSettingsViewModel(workflow, stage, ViewModelContextProvider, _updateStageCard);
                break;
            case StageTypes.CommunityTest:
                CurrentStageSettingsViewModel = new CommunityTestStageSettingsViewModel(workflow, stage, ViewModelContextProvider, _updateStageCard);
                break;
            case StageTypes.ConsultantCheck:
                CurrentStageSettingsViewModel = new ConsultantCheckStageSettingsViewModel(workflow, stage, ViewModelContextProvider, _updateStageCard);
                break;
            case StageTypes.ConsultantApproval:
                CurrentStageSettingsViewModel = new ConsultantApprovalStageSettingsViewModel(workflow, stage, ViewModelContextProvider, _updateStageCard);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<IRoutableViewModel> ProceedWithChanges()
    {
        await Task.Run(CurrentStageSettingsViewModel.ConfirmAsync);
        return await NavigateBack();
    }

    public override void Dispose()
    {
        Workflow = null;
        Stage = null;
        _updateStageCard = null;

        ProceedButtonViewModel?.Dispose();
        ProceedButtonViewModel = null;

        CurrentStageSettingsViewModel?.Dispose();
        CurrentStageSettingsViewModel = null;

        base.Dispose();
    }
}