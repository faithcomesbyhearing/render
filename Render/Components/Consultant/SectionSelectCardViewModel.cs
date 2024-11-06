using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Consultant.ConsultantCheck;

namespace Render.Components.Consultant;

public class SectionSelectCardViewModel :  SectionNavigationViewModel
{
    public Stage Stage { get; private set; }
    public Section Section { get; private set; }
    public string CardTitle { get; }
    public string VerseRange { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateToSectionCommand { get; }

    public SectionSelectCardViewModel(Section section, Stage stage, IViewModelContextProvider viewModelContextProvider)
        : base("SectionSelectCard", viewModelContextProvider)
    {
        Stage = stage;
        Section = section;
        CardTitle = section.Title.Text;
        VerseRange = section.ScriptureReference;

        NavigateToSectionCommand = ReactiveCommand.CreateFromTask(NavigateToConsultantCheck);
    }

    private async Task<IRoutableViewModel> NavigateToConsultantCheck()
    {
        var stageService = ViewModelContextProvider.GetStageService();
        var step = await stageService.GetStepToWorkAsync(Section.Id, RenderStepTypes.ConsultantCheck, GetProjectId());

        // fix for open reviewed section
        if (step == null)
        {
            step = Stage.Steps.FirstOrDefault(s => s.RenderStepType == RenderStepTypes.ConsultantCheck);
        }

        var sectionRepository = ViewModelContextProvider.GetSectionRepository();
        Section = await sectionRepository.GetSectionWithDraftsAsync(Section.Id, true, true, withReferences: true);
        
        if (IsAudioMissing(Section, RenderStepTypes.ConsultantCheck, checkForBackTranslationAudio: true)) return null;

        var sessionService = ViewModelContextProvider.GetSessionStateService();
        sessionService.SetCurrentStep(step.Id, Section.Id);

        var consultantCheckViewModel = await ConsultantCheckViewModel.CreateAsync(ViewModelContextProvider, this, step, Stage);
        return await NavigateTo(consultantCheckViewModel);
    }

    public override void Dispose()
    {
        Section = null;
        Stage = null;
            
        base.Dispose();
    }
}