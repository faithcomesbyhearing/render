using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.PeerReview.PassageListen;
using Render.Pages.PeerReview.SectionListen;

namespace Render.Kernel.NavigationFactories
{
    public class PeerCheckDispatcher : IStepViewModelDispatcher
    {
        public async Task<ViewModelBase> GetViewModelToNavigateTo(IViewModelContextProvider viewModelContextProvider,
            Step step, Section section)
        {
            var sessionService = viewModelContextProvider.GetSessionStateService();
            sessionService.SetCurrentStep(step.Id, section.Id);

            var passage = sessionService.ActiveSession?.PassageNumber == null
                ? section.Passages.First()
                : section.Passages.First(x => x.PassageNumber.Equals(sessionService.ActiveSession.PassageNumber));

            var sessionPage = sessionService.GetSessionPage(step.Id, section.Id, passage.PassageNumber);

            if (!string.IsNullOrEmpty(sessionPage))
            {
                return await GetViewModelFromSessionPage(viewModelContextProvider, sessionPage, step, section, passage);
            }

            return await GetViewModelFromSettings(viewModelContextProvider, step, section, passage);
        }

        private async Task<ViewModelBase> GetViewModelFromSettings(IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage)
        {
            if (step.StepSettings.GetSetting(SettingType.DoSectionReview))
            {
                return await PeerReviewSectionListen.GetViewModelFromIdiomAsync(
                    section,
                    step,
                    viewModelContextProvider);
            }

            return await PeerReviewPassageListen.GetViewModelFromIdiomAsync(
                viewModelContextProvider,
                section,
                passage,
                step);
        }

        private async Task<ViewModelBase> GetViewModelFromSessionPage(
            IViewModelContextProvider viewModelContextProvider,
            string sessionPage,
            Step step,
            Section section,
            Passage passage)
        {
            if (sessionPage.Contains("SectionListen"))
            {
                return await PeerReviewSectionListen.GetViewModelFromIdiomAsync(
                    section,
                    step,
                    viewModelContextProvider);
            }

            return await PeerReviewPassageListen.GetViewModelFromIdiomAsync(
                viewModelContextProvider,
                section,
                passage,
                step);
        }
    }
}