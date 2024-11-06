using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.SectionRepository;
using Render.Services.SnapshotService;
using Render.Services.StageService;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public class WorkflowNavigationIconViewModel : NavigationIconViewModel
{
    public Stage Stage { get; private set; }
    protected Step Step { get; private set; }
    protected IStageService StageService { get; private set; }
    protected ISnapshotService SnapshotService { get; private set; }
    protected ISectionRepository SectionRepository { get; private set; }

    public WorkflowNavigationIconViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Stage stage,
        Step step,
        int sectionsAtStep,
        string title = null)
        : base(
            viewModelContextProvider: viewModelContextProvider,
            title: title ?? GetStepName(step))
    {
        IconImageGlyph = IconMapper.GetIconGlyphForStepType(step.RenderStepType);
        IconName = IconMapper.GetIconNameForStepType(step.RenderStepType);
        StageNumber = stage.StageNumber;
        StageService = viewModelContextProvider.GetStageService();
        SectionRepository = viewModelContextProvider.GetSectionRepository();
        SnapshotService = viewModelContextProvider.GetSnapshotService();
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateOnClickAsync, CanExecute);
        Stage = stage;
        Step = step;
        ActionState = sectionsAtStep > 0 ? ActionState.Required : ActionState.Inactive;
    }

    protected virtual async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var viewModel = await Task.Run(async () =>
        {
            foreach (var sectionId in await SnapshotService.FilterOutConflicts(Step))
            {
                var section = await SectionRepository.GetSectionWithDraftsAsync(sectionId, true,
                    true, true, true);
                if (IsSectionDocumentMissing(sectionId, section))
                {
                    continue;
                }

                if (IsAudioMissing(section, Step.RenderStepType))
                {
                    return null;
                }

                return await WorkflowPageViewModelFactory
                    .GetViewModelToNavigateTo(ViewModelContextProvider, Step, section);
            }

            return null;
        });
        if (viewModel != null)
        {
            return await NavigateToAndReset(viewModel);
        }

        return null;
    }

    protected bool IsSectionDocumentMissing(Guid sectionId, Section section)
    {
        if (section == null)
        {
            LogInfo("Missing section skipped", new Dictionary<string, string>
            {
                { "Section Id", sectionId.ToString() },
                { "Step", Step.RenderStepType.ToString() }
            });
            return true;
        }

        return false;
    }

    public override void Dispose()
    {
        SectionRepository?.Dispose();
        SnapshotService?.Dispose();
        
        Stage = null;
        Step = null;
        StageService = null;
        SnapshotService = null;
        SectionRepository = null;

        base.Dispose();
    }
}