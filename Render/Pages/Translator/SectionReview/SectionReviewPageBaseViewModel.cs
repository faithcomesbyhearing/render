using System.Reactive.Linq;
using Render.Kernel;
using Render.Resources;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using ReactiveUI;
using Render.Pages.Translator.DraftingPage;

namespace Render.Pages.Translator.SectionReview
{
    public class SectionReviewPageBaseViewModel : WorkflowPageBaseViewModel
    {
        public string Title { get; }

        protected SectionReviewPageBaseViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            string urlPathSegment,
            Stage stage) : base(
            urlPathSegment,
            viewModelContextProvider,
            GetStepName(step),
            section,
            stage,
            step,
            secondPageName: AppResources.SectionReview)
        {
            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            Title = string.Format(AppResources.Section, Section.Number);
            ProceedButtonViewModel.SetCommand(NavigateHomeAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            if (Step.RenderStepType == RenderStepTypes.Draft)
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async Task<IRoutableViewModel> NavigateHomeAsync()
        {
            await Task.Run(async () =>
            {
                var sectionMovementService = ViewModelContextProvider.GetSectionMovementService();
                await sectionMovementService.AdvanceSectionAsync(Section, Step, GetProjectId(), GetLoggedInUserId());
            });
            return await NavigateToHomeOnMainStackAsync();
        }

        public async Task<IRoutableViewModel> NavigateToDraftingAsync(Passage passage)
        {
            var draftViewModel = await DraftingViewModel.CreateAsync(Section, passage,
                Step, ViewModelContextProvider, Stage);
            return await NavigateTo(draftViewModel);
        }
    }
}