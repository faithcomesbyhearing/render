using DynamicData;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Extensions;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public class SelectSectionToApproveViewModel : WorkflowPageBaseViewModel
    {
        public static async Task<SelectSectionToApproveViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Stage stage)
        {
            var grandCentralStation = viewModelContextProvider.GetGrandCentralStation();
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            var sectionIds = grandCentralStation.SectionsAtStep(step.Id);
            var sections = new List<Section>();

            foreach (var sectionId in sectionIds)
            {
                sections.Add(await sectionRepository.GetSectionWithDraftsAsync(sectionId));
            }

            return new SelectSectionToApproveViewModel(viewModelContextProvider, sections, stage);
        }

        public DynamicDataWrapper<SectionToApproveCardViewModel> SectionsToApprove = new();

        private SelectSectionToApproveViewModel(
            IViewModelContextProvider viewModelContextProvider,
            List<Section> sections,
            Stage stage) : base(
                urlPathSegment: "SelectApproveSection",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.ConsultantApproval,
                section: null,
                stage: stage,
                secondPageName: AppResources.SectionSelect)
        {
            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
            {
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.ConsultantApproval, color.Color)?.Glyph;
            }

            foreach (var section in sections)
            {
                SectionsToApprove.Add(new SectionToApproveCardViewModel(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: section,
                    removeApproveSectionAction: RemoveApproveSectionFromSelectList));
            }

            Disposables
                .Add(SectionsToApprove.Observable
                .MergeMany(item => item.NavigateToSectionCommand.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));
        }

        private async void RemoveApproveSectionFromSelectList(Section approveSection)
        {
            var sectionToRemove = SectionsToApprove.Items.FirstOrDefault(x => x.Section.Id.Equals(approveSection.Id));
            if (sectionToRemove != null)
            {
                SectionsToApprove.Remove(sectionToRemove);
            }
            if (SectionsToApprove.SourceItems.IsNullOrEmpty())
            {
                await NavigateToHomeOnMainStackAsync();
            }
            else
            {
                NavigateBackCommand.Execute();
            }
        }

        public override void Dispose()
        {
            SectionsToApprove?.Dispose();
            SectionsToApprove = null;

            base.Dispose();
        }
    }
}