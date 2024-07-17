using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowManagement;

public class StepLabelViewModel : ViewModelBase
{
    public string Title { get; }

    [Reactive] public bool ShowSeparator { get; set; } = true;

    public StepLabelViewModel(RenderStepTypes stepType, IViewModelContextProvider viewModelContextProvider, Guid stageId)
        : base("StepLabel", viewModelContextProvider)
    {
        switch (stepType)
        {
            case RenderStepTypes.Draft:
                Title = GetStepName(viewModelContextProvider, RenderStepTypes.Draft, stageId);
                break;
            case RenderStepTypes.PeerCheck:
                Title = GetStepName(viewModelContextProvider, RenderStepTypes.PeerCheck, stageId);
                break;
            case RenderStepTypes.PeerRevise:
                Title = AppResources.PeerRevise;
                break;
            case RenderStepTypes.CommunitySetup:
                Title = AppResources.CommunitySetup;
                break;
            case RenderStepTypes.CommunityTest:
                Title = GetStepName(viewModelContextProvider, RenderStepTypes.CommunityTest, stageId);
                break;
            case RenderStepTypes.CommunityRevise:
                Title = AppResources.CommunityRevise;
                break;
            case RenderStepTypes.BackTranslate:
                Title = AppResources.BackTranslate;
                break;
            case RenderStepTypes.InterpretToConsultant:
                Title = AppResources.InterpretToConsultant;
                break;
            case RenderStepTypes.InterpretToTranslator:
                Title = AppResources.InterpretToTranslator;
                break;
            case RenderStepTypes.Transcribe:
                Title = AppResources.Transcribe;
                break;
            case RenderStepTypes.ConsultantCheck:
                Title = GetStepName(viewModelContextProvider, RenderStepTypes.ConsultantCheck, stageId);
                break;
            case RenderStepTypes.ConsultantRevise:
                Title = AppResources.ConsultantRevise;
                break;
            case RenderStepTypes.ConsultantApproval:
                Title = AppResources.ConsultantApproval;
                break;
            default:
                Title = "Generic";
                break;
        }
    }
}