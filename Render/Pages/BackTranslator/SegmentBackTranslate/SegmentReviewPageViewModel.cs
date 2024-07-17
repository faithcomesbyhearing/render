using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Components.BarPlayer;
using ReactiveUI.Fody.Helpers;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel.NavigationFactories;
using Render.Models.Project;
using Render.Pages.BackTranslator.RetellBackTranslate;
using Render.Resources;

namespace Render.Pages.BackTranslator.SegmentBackTranslate
{
    public class SegmentReviewPageViewModel : WorkflowPageBaseViewModel
    {
        private Passage _passage;
        private SegmentBackTranslation _segmentBackTranslation;
        private readonly string _segmentName;
        private readonly string _projectLanguageName;
        
        [Reactive] public IMiniWaveformPlayerViewModel SegmentPlayer { get; private set; }
        [Reactive] public IBarPlayerViewModel TwoStepSegmentPlayer { get; set; }
        [Reactive] public IBarPlayerViewModel BackTranslatePlayer { get; private set; }

        [Reactive] public bool IsTwoStepBackTranslate { get; private set; }

        public static async Task<SegmentReviewPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage)
        {
            var renderProjectRepository = viewModelContextProvider.GetPersistence<RenderProject>();
            var project = await renderProjectRepository.QueryOnFieldAsync("ProjectId", section.ProjectId.ToString());

            var pageVm = new SegmentReviewPageViewModel(
                viewModelContextProvider,
                step,
                section,
                passage,
                segmentBackTranslation,
                segmentName,
                stage,
                project);

            pageVm.Initialize();

            return pageVm;
        }

        private SegmentReviewPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage,
            RenderProject project)
            : base(
                urlPathSegment: "SegmentReview",
                viewModelContextProvider: viewModelContextProvider,
                pageName: AppResources.BackTranslate,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                nonDraftTranslationId: segmentBackTranslation.Id,
                secondPageName: AppResources.SegmentReview)
        {
            _passage = passage;
            _segmentBackTranslation = segmentBackTranslation;
            _segmentName = segmentName;
            _projectLanguageName = project.GetLanguageName();
            
            IsTwoStepBackTranslate = Step.Role == Roles.BackTranslate2;

            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.SegmentBackTranslate,
                    ResourceExtensions.GetColor("SecondaryText"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack);

            SetProceedButtonIcon();
        }

        private void Initialize()
        {
            SetupSegmentPlayer();
            SetupBackTranslatePlayer();
        }

        private void SetupSegmentPlayer()
        {
            var requirePassageReview = Step.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageReview);
            var actionState = requirePassageReview ? ActionState.Required : ActionState.Optional;

            // if we are on the 2-step back translate then the consultant language of the first step is our language name
            var languageName = IsTwoStepBackTranslate
                ? Step.StepSettings.GetString(SettingType.ConsultantLanguage)
                : _projectLanguageName;

            var projectAutonym = string.IsNullOrWhiteSpace(languageName)
                ? _segmentName
                : $"{languageName} - {_segmentName}";

            if (IsTwoStepBackTranslate)
            {
                TwoStepSegmentPlayer = ViewModelContextProvider.GetBarPlayerViewModel(
                    _segmentBackTranslation,
                    actionState,
                    projectAutonym, 0);

                ActionViewModelBaseSourceList.Add(TwoStepSegmentPlayer);
            }
            else
            {
                SegmentPlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel
                (
                    _passage.CurrentDraftAudio,
                    actionState,
                    projectAutonym,
                    _segmentBackTranslation.TimeMarkers
                );

                ActionViewModelBaseSourceList.Add(SegmentPlayer);
            }
        }

        private void SetupBackTranslatePlayer()
        {
            var requirePassageReview = Step.StepSettings.GetSetting(SettingType.RequireSegmentBTPassageReview);
            var actionState = requirePassageReview ? ActionState.Required : ActionState.Optional;

            var consultantLanguage = Step.Role == Roles.BackTranslate
                ? Step.StepSettings.GetString(SettingType.SegmentConsultantLanguage)
                : Step.StepSettings.GetString(SettingType.SegmentConsultant2StepLanguage);

            var translationTitle = consultantLanguage != "" ? consultantLanguage : AppResources.Translation;

            var recordedAudio = IsTwoStepBackTranslate
                ? _segmentBackTranslation.RetellBackTranslationAudio
                : (Draft)_segmentBackTranslation;

            BackTranslatePlayer = ViewModelContextProvider.GetBarPlayerViewModel(
                recordedAudio,
                actionState,
                translationTitle, 1);

            ActionViewModelBaseSourceList.Add(BackTranslatePlayer);
        }

        private async Task<IRoutableViewModel> NavigateAsync()
        {
            try
            {
                if (Step.Role != Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0 &&
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s => s.HasAudio)) ||
                    Step.Role == Roles.BackTranslate2 && Section.Passages.All(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0 &&
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s =>
                            s.RetellBackTranslationAudio != null && s.RetellBackTranslationAudio.HasAudio)))
                {
                    await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);

                    return await NavigateToHomeOnMainStackAsync();
                }

                /*After completing SBT recording for the segment , user is navigated to the 'Passage Listen' screen
                 for selecting new segment for processing work.*/
                if (_passage.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s => !s.HasAudio))
                {
                    var selectPageViewModel = await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                        section: Section,
                        step: Step,
                        passage: _passage,
                        viewModelContextProvider: ViewModelContextProvider,
                        selectedSegment: _segmentBackTranslation);

                    return await NavigateToAndReset(selectPageViewModel);
                }

                if (Step.Role == Roles.BackTranslate2 &&
                    _passage.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s =>
                        s.RetellBackTranslationAudio == null || !s.RetellBackTranslationAudio.HasAudio))
                {
                    var selectPageViewModel = await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                        section: Section,
                        step: Step,
                        passage: _passage,
                        viewModelContextProvider: ViewModelContextProvider,
                        selectedSegment: _segmentBackTranslation);
                        

                    return await NavigateToAndReset(selectPageViewModel);
                }

                /* When the last segment from the passage is translated, then the next passage is opened for SBT
                 (or if it is the last passage, then the section is moved to the next step).*/
                Passage nextPassage;

                if (Step.Role == Roles.BackTranslate2)
                {
                    nextPassage = Section.Passages.First(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s =>
                            s.RetellBackTranslationAudio == null || !s.RetellBackTranslationAudio.HasAudio) ||
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count == 0);
                }
                else
                {
                    nextPassage = Section.Passages.First(x =>
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Any(s => !s.HasAudio) ||
                        x.CurrentDraftAudio.SegmentBackTranslationAudios.Count == 0);
                }

                if (Step.StepSettings.GetSetting(SettingType.DoRetellBackTranslate))
                {
                    if (nextPassage.CurrentDraftAudio.RetellBackTranslationAudio == null)
                    {
                        var newRetellBackTranslation = new RetellBackTranslation(nextPassage.CurrentDraftAudio.Id,
                            Guid.Empty, Guid.Empty, Section.ProjectId, Section.ScopeId);
                        await ViewModelContextProvider.GetRetellBackTranslationRepository()
                            .SaveAsync(newRetellBackTranslation);
                        nextPassage.CurrentDraftAudio.RetellBackTranslationAudio = newRetellBackTranslation;
                    }

                    var viewModel = await RetellBackTranslateResolver.GetRetellPassageTranslateViewModelAsync(Section,
                        nextPassage, nextPassage.CurrentDraftAudio.RetellBackTranslationAudio, Step,
                        ViewModelContextProvider);

                    return await NavigateToAndReset(viewModel);
                }

                var selectPage = await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(Section,
                    Step, nextPassage, ViewModelContextProvider);

                return await NavigateToAndReset(selectPage);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        protected sealed override void SetProceedButtonIcon()
        {
            if (Step.Role != Roles.BackTranslate2 && Section.Passages.All(x =>
                    x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0 &&
                    x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s => s.HasAudio)) ||
                Step.Role == Roles.BackTranslate2 && Section.Passages.All(x =>
                    x.CurrentDraftAudio.SegmentBackTranslationAudios.Count > 0 &&
                    x.CurrentDraftAudio.SegmentBackTranslationAudios.All(s =>
                        s.RetellBackTranslationAudio != null && s.RetellBackTranslationAudio.HasAudio)))
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private new async Task<IRoutableViewModel> NavigateBack()
        {
            try
            {
                IsLoading = true;

                var vm = await SegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(Section,
                    _passage, Stage, Step, _segmentBackTranslation,
                    BackTranslateDispatcher.GetSegmentNumber(_passage, _segmentBackTranslation.Id),
                    ViewModelContextProvider);

                IsLoading = false;

                return await NavigateToAndReset(vm);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            SegmentPlayer?.Dispose();
            SegmentPlayer = null;

            TwoStepSegmentPlayer?.Dispose();
            TwoStepSegmentPlayer = null;

            BackTranslatePlayer?.Dispose();
            BackTranslatePlayer = null;

            ProceedButtonViewModel?.Dispose();
            ProceedButtonViewModel = null;

            _segmentBackTranslation = null;
            _passage = null;

            base.Dispose();
        }
    }
}