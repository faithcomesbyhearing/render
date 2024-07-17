using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Revise.NoteListen;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.PassageListen;
using Render.Pages.CommunityTester.CommunityCheckRevise;
using Render.Resources;
using System.Reactive.Linq;

namespace Render.Pages.Translator.PassageReview
{
    public class PassageReviewPageBaseViewModel : WorkflowPageBaseViewModel
    {
        protected Passage Passage { get; set; }

        protected PassageReviewPageBaseViewModel(
            IViewModelContextProvider viewModelContextProvider,
            string pageName,
            string secondPageName,
            Section section,
            Passage passage,
            Step step,
            string urlPathSegment,
            Stage stage) :
            base(
                urlPathSegment,
                viewModelContextProvider,
                pageName,
                section,
                stage,
                step,
                passage.PassageNumber,
                secondPageName: secondPageName)
        {
            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            Passage = passage;
            ProceedButtonViewModel.SetCommand(NavigateAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            if (Step.RenderStepType == RenderStepTypes.Draft)
            {
                if (Section.Passages.All(x => x.HasAudio)
                    && !Step.StepSettings.GetSetting(SettingType.DoSectionReview)
                    && Section.GetNextPassage(Passage) == null)
                {
                    ProceedButtonViewModel.IsCheckMarkIcon = true;
                }
            }
        }

        protected async Task NavigateToReRecord()
        {
            IsLoading = true;
            var viewModel = await Task.Run(async () => await DraftingViewModel.CreateAsync(Section, Passage,
                Step, ViewModelContextProvider, Stage, false));
            await NavigateTo(viewModel);
            IsLoading = false;
        }

        protected IBarPlayerViewModel PopulateReference(SectionReferenceAudio referenceAudio, Passage passage,
            bool passageReviewRequired, int playerNumber)
        {
            var isRequired = referenceAudio.Reference.Primary && passageReviewRequired;
            var timeMarkers = referenceAudio.PassageReferences.FirstOrDefault(x =>
                x.PassageNumber.Equals(passage.PassageNumber))?.TimeMarkers;
            return ViewModelContextProvider.GetBarPlayerViewModel(referenceAudio,
                isRequired ? ActionState.Required : ActionState.Optional, referenceAudio.Reference.Name, playerNumber, timeMarkers);
        }

        private async Task<IRoutableViewModel> NavigateAsync()
        {
            ViewModelBase vm;
            if (Step.RenderStepType == RenderStepTypes.Draft)
            {
                var nextPassage = Section.GetNextPassage(Passage);
                if (nextPassage != null && !Section.Passages.All(x => x.HasAudio))
                {
                    if (Step.StepSettings.GetSetting(SettingType.DoPassageListen))
                    {
                        vm = await Task.Run(async () => await PassageListenViewModel.CreateAsync(ViewModelContextProvider,
                            Section, Step, nextPassage, Stage));
                    }
                    else
                    {
                        vm = await Task.Run(async () => await DraftingViewModel.CreateAsync(Section, nextPassage,
                            Step, ViewModelContextProvider, Stage));
                    }

                    return await NavigateToAndReset(vm);
                }

                if (Step.StepSettings.GetSetting(SettingType.DoSectionReview))
                {
                    vm = await Task.Run(async () => await SectionReview.SectionReview.GetSectionReviewViewModelAsync(Section,
                        ViewModelContextProvider, Step));
                    return await NavigateTo(vm);
                }

                await Task.Run(async () => { await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step); });
                return await NavigateToHomeOnMainStackAsync();
            }

            //Else we're revising
            if (Step.RenderStepType == RenderStepTypes.CommunityRevise)
            {
                vm = await Task.Run(async () => await CommunityRevisePageViewModel.CreateAsync(ViewModelContextProvider, Section, Step, Stage));
                return await NavigateToAndReset(vm);
            }
            
            vm = await Task.Run(async () => await NoteListen.GetNoteListenViewModelAsync(ViewModelContextProvider, Section, Step));
            return await NavigateToAndReset(vm);
        }

        public override void Dispose()
        {
            Passage = null;

            base.Dispose();
        }
    }
}