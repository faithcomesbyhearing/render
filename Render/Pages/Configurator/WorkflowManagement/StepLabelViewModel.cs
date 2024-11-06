using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;

namespace Render.Pages.Configurator.WorkflowManagement;

public class StepLabelViewModel : ViewModelBase
{
    public string Title { get; }

    [Reactive] public bool ShowSeparator { get; set; } = true;

    public StepLabelViewModel(
        Step step,
        IViewModelContextProvider viewModelContextProvider,
        Stage stage)
        : base(
            urlPathSegment: "StepLabel",
            viewModelContextProvider: viewModelContextProvider)
    {
        switch (step.RenderStepType)
        {
            case RenderStepTypes.InterpretToConsultant:
                Title = step.GetName(AppResources.InterpretToConsultant);
                break;
            case RenderStepTypes.InterpretToTranslator:
                Title = step.GetName(AppResources.InterpretToTranslator);
                break;
            case RenderStepTypes.Unknown:
                Title = step.GetName("Generic");
                break;
            default:
                Title = step.GetName();
                break;
        }
    }
}