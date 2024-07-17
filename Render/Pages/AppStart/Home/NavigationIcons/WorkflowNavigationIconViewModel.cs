using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.NavigationFactories;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.SectionRepository;
using Render.Services;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public class WorkflowNavigationIconViewModel : NavigationIconViewModel
{
    public Stage Stage { get; private set; }
    protected Step Step { get; private set; }
    protected IGrandCentralStation GrandCentralStation { get; private set; }
    protected ISectionRepository _sectionRepository { get; private set; }

    public WorkflowNavigationIconViewModel(IViewModelContextProvider viewModelContextProvider, Stage stage,
        Step step, int sectionsAtStep, string title = null)
        : base(viewModelContextProvider,
            title ?? GetStepName(viewModelContextProvider, step.RenderStepType, stage.Id))
    {
        IconImageGlyph = IconMapper.GetIconGlyphForStepType(step.RenderStepType);
        IconName = IconMapper.GetIconNameForStepType(step.RenderStepType);
        StageNumber = stage.StageNumber;
        GrandCentralStation = viewModelContextProvider.GetGrandCentralStation();
        _sectionRepository = viewModelContextProvider.GetSectionRepository();
        NavigateToPageCommand = ReactiveCommand.CreateFromTask(NavigateOnClickAsync, CanExecute);
        Stage = stage;
        Step = step;
        ActionState = sectionsAtStep > 0 ? ActionState.Required : ActionState.Inactive;
    }

    protected virtual async Task<IRoutableViewModel> NavigateOnClickAsync()
    {
        var viewModel = await Task.Run(async () =>
        {
            foreach (var sectionId in await GrandCentralStation.FilterOutConflicts(Step))
            {
                var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, true,
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
            return await HostScreen.Router.NavigateAndReset.Execute(viewModel);
        }

        return null;
    }

    protected bool IsSectionDocumentMissing(Guid sectionId, Section section)
    {
        if (section == null)
        {
            LogInfo("Missing section skipped", new Dictionary<string, string>
                {
                    {"Section Id", sectionId.ToString()},
                    {"Step", Step.RenderStepType.ToString()}
                });
            return true;
        }

        return false;
    }

    public override void Dispose()
    {
        _sectionRepository?.Dispose();

        Stage = null;
        Step = null;
        GrandCentralStation = null;
        _sectionRepository = null;

        base.Dispose();
    }
}