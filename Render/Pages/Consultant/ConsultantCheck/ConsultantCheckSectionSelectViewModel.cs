using System.Reactive;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Consultant;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Pages.Consultant.ConsultantCheck;

public class ConsultantCheckSectionSelectViewModel : WorkflowPageBaseViewModel
    {
        public readonly DynamicDataWrapper<SectionSelectCardViewModel> SectionsToCheck =
            new DynamicDataWrapper<SectionSelectCardViewModel>();

        public readonly DynamicDataWrapper<SectionSelectCardViewModel> CheckedSections =
            new DynamicDataWrapper<SectionSelectCardViewModel>();

        public bool HasCheckedSections { get; }
        public bool HasUncheckedSections { get; }

        [Reactive] public bool ReviewedSelected { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> SelectUnreviewedCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> SelectReviewedCommand { get; set; }

        public static async Task<ConsultantCheckSectionSelectViewModel> CreateAsync(
            Guid projectId, 
            IViewModelContextProvider viewModelContextProvider, 
            Stage stage, 
            Step step)
        {
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            var allSections = await sectionRepository.GetSectionsForProjectAsync(projectId);
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            
            //if a section is in the consultant check step, it belongs in unreviewed list
            var unreviewedSectionIds = new List<Guid>();
            unreviewedSectionIds.AddRange( grandCentralStation.SectionsAtStep(step.Id)); 

            var reviseSteps = stage?.Steps.Where(s => s.RenderStepType == RenderStepTypes.ConsultantRevise).ToList();
            var consultantCheckSteps = stage?.Steps.Where(s =>
                s.RenderStepType == RenderStepTypes.InterpretToConsultant ||
                s.RenderStepType == RenderStepTypes.InterpretToTranslator).ToList();
            var workflowEntrySteps = stage?.GetAllWorkflowEntrySteps().Where( s =>
                s.RenderStepType == RenderStepTypes.BackTranslate ||
                s.RenderStepType == RenderStepTypes.Transcribe).ToList();
            consultantCheckSteps?.AddRange(workflowEntrySteps);
            
            var reviewedSectionIds = new List<Guid>();
            
            if (reviseSteps != null)
            {
                foreach (var reviseStep in reviseSteps)
                {
                    //if a section is in the consultant revise step, it belongs in reviewed list
                    var sectionIds =
                        grandCentralStation.GetAllAssignedSectionAtStep(reviseStep.Id, reviseStep.RenderStepType);
                    reviewedSectionIds.AddRange(sectionIds);
                }
            }
            
            if (consultantCheckSteps != null)
            {
                foreach (var sectionId in consultantCheckSteps
                             .Select(checkStep => grandCentralStation
                             .GetAllAssignedSectionAtStep(checkStep.Id, checkStep.RenderStepType))
                             .SelectMany(sectionIds => sectionIds))
                {
                    var section = await sectionRepository.GetSectionAsync(sectionId);
                    if (section?.CheckedBy != default(Guid))
                    {
                        reviewedSectionIds.Add(sectionId);
                    }
                }
            }

            var unreviewedSections = allSections.Where(x => unreviewedSectionIds.Contains(x.Id)).
                OrderBy(x => x.Number).ToList();
            var reviewedSections = allSections.Where(x => reviewedSectionIds.Contains(x.Id))
                .OrderBy(x => x.Number).ToList();

            foreach (var section in unreviewedSections.Where(section => section.CheckedBy != default))
            {
                section.SetCheckedBy(default);
                await sectionRepository.SaveSectionAsync(section);
            }

            var consultantCheckViewModel =
                new ConsultantCheckSectionSelectViewModel(viewModelContextProvider, stage, step, unreviewedSections,
                    reviewedSections);
            consultantCheckViewModel.ProceedButtonViewModel.SetCommand(consultantCheckViewModel.NavigateToHomeOnMainStackAsync);
            
            return consultantCheckViewModel;
        }
        
        private ConsultantCheckSectionSelectViewModel(
                IViewModelContextProvider viewModelContextProvider,
                Stage stage,
                Step step,
                List<Section> unreviewedSections,
                List<Section> reviewedSections) : 
            base(
                urlPathSegment: "ConsultantCheckSectionSelect",
                viewModelContextProvider,
                pageName: AppResources.ConsultantCheck,
                section: null,
                stage,
                step,
                secondPageName: AppResources.SectionSelect)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = 
                    IconExtensions.BuildFontImageSource(Icon.ConsultantCheckOriginal, color.Color, size:35)?.Glyph;   
            }
           
            foreach (var section in unreviewedSections)
            {
                SectionsToCheck.Add(new SectionSelectCardViewModel(section, stage, ViewModelContextProvider));
                HasUncheckedSections = true;
            }

            foreach (var section in reviewedSections)
            {
                CheckedSections.Add(new SectionSelectCardViewModel(section, stage, ViewModelContextProvider));
                HasCheckedSections = true;
            }
            
            Disposables.Add(CheckedSections.Observable
                .MergeMany(item => item.NavigateToSectionCommand.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));

            Disposables.Add(SectionsToCheck.Observable
                .MergeMany(item => item.NavigateToSectionCommand.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));

            SelectReviewedCommand = ReactiveCommand.Create(SelectReviewed);
            SelectUnreviewedCommand = ReactiveCommand.Create(SelectUnreviewed);
        }

        private void SelectUnreviewed()
        {
            ReviewedSelected = false;
        }

        private void SelectReviewed()
        {
            ReviewedSelected = true;
        }

        public override void Dispose()
        {
            SectionsToCheck?.Dispose();
            CheckedSections?.Dispose();
        
            base.Dispose();
        }
    }