using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.TitleBar;
using Render.Components.TitleBar.MenuActions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;

namespace Render.Kernel
{
    public class PageViewModelBase : ViewModelBase
    {
        [Reactive] public ITitleBarViewModel TitleBarViewModel { get; set; }

        protected PageViewModelBase(string urlPathSegment, 
            IViewModelContextProvider viewModelContextProvider, 
            string pageName,
            List<IMenuActionViewModel> menuActionViewModels = null,
            int sectionNumber = 0, 
            Audio sectionTitleAudio = null,
            bool canNavigateBack = true,
            Section section = null,
            PassageNumber passageNumber = null,
            Stage stage = null,
            Step step = null,
            bool isSegmentSelect = false,
            string secondPageName="") :
            base(urlPathSegment, viewModelContextProvider)
        {
            var elementsToActivate = new List<TitleBarElements>();
            switch (urlPathSegment)
            {
                case "Login":
                    break;
                case "ProjectSelect":
                    elementsToActivate.AddRange(new List<TitleBarElements>
                    {
                        TitleBarElements.RenderLogo,
                        TitleBarElements.SettingsButton,
                        TitleBarElements.PageTitle
                    });
                    break;
                case "Home":
                    elementsToActivate.AddRange(new List<TitleBarElements>
                    {
                        TitleBarElements.RenderLogo,
                        TitleBarElements.SettingsButton,
                        TitleBarElements.PageTitle
                    });

                    if (canNavigateBack)
                    {
                        elementsToActivate.Add(TitleBarElements.BackButton);
                    }
                    break;
                default:
                    elementsToActivate.AddRange(new List<TitleBarElements>
                    {
                        TitleBarElements.RenderLogo,
                        TitleBarElements.PageTitle,
                        TitleBarElements.SectionPlayer
                    });

                    if (canNavigateBack)
                    {
                        elementsToActivate.Add(TitleBarElements.BackButton);
                    }

                    break;
            }
            
            menuActionViewModels = menuActionViewModels ?? new List<IMenuActionViewModel>();
            var titleBarMenuViewModel = new TitleBarMenuViewModel(
                menuActionViewModels,
                viewModelContextProvider,
                urlPathSegment,
                pageName,
                section,
                passageNumber,
                stage,
                step,
                isSegmentSelect);

            TitleBarViewModel = viewModelContextProvider.GetTitleBarViewModel(elementsToActivate,
                titleBarMenuViewModel,
                ViewModelContextProvider, 
                pageName,
                sectionTitleAudio, 
                sectionNumber, 
                secondPageName,
                passageNumber?.PassageNumberString);

            Disposables.Add(this.WhenAnyValue(
                x => x.TitleBarViewModel.TitleBarMenuViewModel.IsLoading,
                x => x.IsLoading)
                .Subscribe(value => IsLoading = value.Item1 || value.Item2));

            TryStopPlaybackActivity();
        }

        /// <summary>
        /// Stops playback only and ignores recording, 
        /// due to the asynchronious nature of stoppting recording process.
        /// </summary>
        private void TryStopPlaybackActivity()
        {
            var activityService = ViewModelContextProvider.GetAudioActivityService();
            if (activityService.IsAudioRecording)
            {
                return;
            }

            activityService.Stop();
        }

        public void PauseSectionTitlePlayer()
        {
            if (TitleBarViewModel != null && 
                TitleBarViewModel.SectionTitlePlayerViewModel != null)
            {
                TitleBarViewModel.SectionTitlePlayerViewModel.Pause();
            }
        }

        public override void Dispose()
        {
            TitleBarViewModel?.Dispose();

            base.Dispose();
        }
    }
}