using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Project;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.BackTranslator.SegmentBackTranslate;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.BackTranslator.RetellBackTranslate
{
    public class RetellPassageReviewPageViewModel : WorkflowPageBaseViewModel
    {
        private Passage _passage;
        private readonly string _passageName;
        private readonly string _projectLanguageName;
        
        [Reactive] public IMiniWaveformPlayerViewModel PassagePlayer { get; private set; }
        [Reactive] public IBarPlayerViewModel TwoStepPassagePlayer { get; private set; }
        [Reactive] public IBarPlayerViewModel BackTranslatePlayer { get; private set; }
        
        [Reactive] public bool IsTwoStepBackTranslate { get; private set; }
        
        public static async Task<RetellPassageReviewPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            string passageName,
            Stage stage)
        {
            var renderProjectRepository = viewModelContextProvider.GetPersistence<RenderProject>();
            var project = await renderProjectRepository.QueryOnFieldAsync("ProjectId", section.ProjectId.ToString());

            var pageVm = new RetellPassageReviewPageViewModel(
                viewModelContextProvider,
                step,
                section,
                passage,
                retellBackTranslation,
                passageName,
                stage,
                project);
            
            pageVm.Initialize();

            return pageVm;
        }

        private RetellPassageReviewPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            string passageName,
            Stage stage,
            RenderProject project)
            : base(
                urlPathSegment: "RetellBTPassageReview",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                secondPageName: AppResources.PassageReview)
        {
            _passage = passage;
            _passageName = passageName;
            _projectLanguageName = project.GetLanguageName();

            IsTwoStepBackTranslate = step.Role == Roles.BackTranslate2;
            
            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.RetellBackTranslate,
                    ResourceExtensions.GetColor("SecondaryText"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));

            NavigateBackCommand = ReactiveCommand.CreateFromTask(NavigateBack);

            SetProceedButtonIcon();
        }

        private void Initialize()
        {
            SetupPassagePlayer();
            SetupBackTranslatePlayer();
        }
        
        private void SetupPassagePlayer()
        {
            var requirePassageReview = Step.StepSettings.GetSetting(SettingType.RequireRetellBTPassageReview);
            var actionState = requirePassageReview ? ActionState.Required : ActionState.Optional;

            // if we are on the 2-step back translate then the consultant language of the first step is our language name
            var languageName = IsTwoStepBackTranslate 
                ? Step.StepSettings.GetString(SettingType.ConsultantLanguage) 
                : _projectLanguageName;

            var projectAutonym = string.IsNullOrWhiteSpace(languageName)
                ? _passageName
                : $"{languageName} - {_passageName}";
            
            if (IsTwoStepBackTranslate)
            {
                TwoStepPassagePlayer = ViewModelContextProvider.GetBarPlayerViewModel(
                    _passage.CurrentDraftAudio.RetellBackTranslationAudio,
                    actionState,
                    projectAutonym, 0);
                
                ActionViewModelBaseSourceList.Add(TwoStepPassagePlayer);
            }
            else
            {
                PassagePlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel
                (
                    _passage.CurrentDraftAudio,
                    actionState,
                    projectAutonym
                );
                
                ActionViewModelBaseSourceList.Add(PassagePlayer);
            }
        }

        private void SetupBackTranslatePlayer()
        {
            var requirePassageReview = Step.StepSettings.GetSetting(SettingType.RequireRetellBTPassageReview);
            var actionState = requirePassageReview ? ActionState.Required : ActionState.Optional;
            
            var consultantLanguage = Step.Role == Roles.BackTranslate
                ? Step.StepSettings.GetString(SettingType.ConsultantLanguage)
                : Step.StepSettings.GetString(SettingType.Consultant2StepLanguage);

            var translationTitle = consultantLanguage != "" ? consultantLanguage : AppResources.Translation;
            
            var recordedAudio = IsTwoStepBackTranslate
                ? _passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio
                : _passage.CurrentDraftAudio.RetellBackTranslationAudio;
 
            BackTranslatePlayer = ViewModelContextProvider.GetBarPlayerViewModel(
                recordedAudio,
                actionState,
                translationTitle, 1);
           
            ActionViewModelBaseSourceList.Add(BackTranslatePlayer);
        }
        
        protected sealed override void SetProceedButtonIcon()
        {
            if (!Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate))
            {
                ProceedButtonViewModel.IsCheckMarkIcon = AreStepBackTranslationsRecorded();
            }
        }

        private async Task<IRoutableViewModel> NavigateAsync()
        {
            try
            {
                if (Step.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate))
                {
                    var viewModel = await SegmentBackTranslateResolver.GetSegmentSelectPageViewModel(Section, Step,
                        _passage, ViewModelContextProvider);
                    
                    return await NavigateToAndReset(viewModel);
                }
                if (AreStepBackTranslationsRecorded())
                {
                    var sectionMovementService = ViewModelContextProvider.GetSectionMovementService();
                    await sectionMovementService.AdvanceSectionAsync(Section, Step, GetProjectId(), GetLoggedInUserId()); 
                    
                    return await NavigateToHomeOnMainStackAsync();
                }

                var selectPage = await RetellBackTranslateResolver.GetRetellPassageSelectViewModelAsync(
                    Section, Step, ViewModelContextProvider);
                
                return await NavigateToAndReset(selectPage);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private new async Task<IRoutableViewModel> NavigateBack()
        {
            try
            {
                IsLoading = true;
                
                var vm = await RetellBackTranslateResolver.GetRetellPassageTranslateViewModelAsync(Section, _passage,
                    _passage.CurrentDraftAudio.RetellBackTranslationAudio, Step, ViewModelContextProvider);
                
                IsLoading = false;
                
                return await NavigateToAndReset(vm);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private bool AreStepBackTranslationsRecorded()
        {
            return IsTwoStepBackTranslate
                ? Section.Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio?.HasAudio ?? false)
                : Section.Passages.All(x => x.CurrentDraftAudio?.RetellBackTranslationAudio?.HasAudio ?? false);
        }

        public override void Dispose()
        {
            PassagePlayer?.Dispose();
            PassagePlayer = null;
            
            TwoStepPassagePlayer?.Dispose();
            TwoStepPassagePlayer = null;
            
            BackTranslatePlayer?.Dispose();
            BackTranslatePlayer = null;
            
            ProceedButtonViewModel?.Dispose();
            ProceedButtonViewModel = null;

            _passage = null;

            base.Dispose();
        }
    }
}