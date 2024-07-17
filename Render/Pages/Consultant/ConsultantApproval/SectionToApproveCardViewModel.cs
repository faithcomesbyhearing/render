using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Resources.Localization;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public class SectionToApproveCardViewModel : SectionNavigationViewModel
    {
        private string CardTitle { get; }

        public Section Section { get; private set; }

        public ReactiveCommand<Unit, IRoutableViewModel> NavigateToSectionCommand { get; }

        public Action<Section> RemoveApproveSectionAction { get; }

        public SectionToApproveCardViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Action<Section> removeApproveSectionAction) :
            base("ApproveSection", viewModelContextProvider)
        {
            Section = section;
            RemoveApproveSectionAction = removeApproveSectionAction;

            //TODO remove later when back translation implemented with real data
            var hasRetell = false;
            var hasSegment = false;
            if (section.Passages.All(x => x.CurrentDraftAudio.RetellBackTranslationAudio != null))
            {
                hasRetell = true;
            }

            if (section.Passages.All(x => x.CurrentDraftAudio.SegmentBackTranslationAudios.Count != 0))
            {
                hasSegment = true;
            }

            var hasBoth = hasRetell && hasSegment;
            var textToAppend = hasBoth ? "(Segment and Retell)" : hasRetell ? "(Retell)" : hasSegment ? "(Segment)" : "";

            CardTitle = string.Format(AppResources.ApproveSection, section.Number);
            CardTitle += textToAppend;

            NavigateToSectionCommand = ReactiveCommand.CreateFromTask(NavigateToApproveSectionAsync);

            Disposables
                .Add(NavigateToSectionCommand.IsExecuting
                    .Subscribe(isExecuting => IsLoading = isExecuting));
        }

        private async Task<IRoutableViewModel> NavigateToApproveSectionAsync()
        {
            if (IsAudioMissing(Section, RenderStepTypes.ConsultantApproval, checkForBackTranslationAudio: true)) return null;

            var vm = await ApproveSectionViewModel.CreateAsync(ViewModelContextProvider, Section.Id, RemoveApproveSectionAction);
            return await NavigateTo(vm);
        }

        public override void Dispose()
        {
            Section = null;

            base.Dispose();
        }
    }
}