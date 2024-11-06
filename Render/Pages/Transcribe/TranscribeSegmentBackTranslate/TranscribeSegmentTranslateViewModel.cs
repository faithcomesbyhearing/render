using Render.Components.TranscribeTextBox;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Resources;
using Render.Resources.Styles;
using Render.Services.AudioServices;

namespace Render.Pages.Transcribe.TranscribeSegmentBackTranslate
{
    public class TranscribeSegmentTranslateViewModel : WorkflowPageBaseViewModel
    {
        private SegmentBackTranslation SegmentBackTranslation { get; set; }
        [Reactive] public TranscribeTextBoxViewModel TranscribeTextBoxViewModel { get; private set; }
        [Reactive] public bool LoopAudio { get; set; }
        public IBarPlayerViewModel BarPlayerViewModel { get; }

        public static async Task<TranscribeSegmentTranslateViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage)
        {
            var pageTitle = GetStepName(step);

            var transcriptionText = step.Role == Roles.Transcribe2
                ? segmentBackTranslation.RetellBackTranslationAudio.Transcription ?? string.Empty
                : segmentBackTranslation.Transcription ?? string.Empty;

            var transcriptionWindowViewModel = await TranscribeTextBoxViewModel.CreateAsync(viewModelContextProvider,
                transcriptionText);

            var pageViewModel = new TranscribeSegmentTranslateViewModel(
                viewModelContextProvider,
                pageTitle,
                step,
                section,
                passage,
                transcriptionWindowViewModel,
                segmentBackTranslation,
                segmentName,
                stage);

            return pageViewModel;
        }

        private TranscribeSegmentTranslateViewModel(
            IViewModelContextProvider viewModelContextProvider,
            string pageName,
            Step step,
            Section section,
            Passage passage,
            TranscribeTextBoxViewModel transcribeTextBoxViewModel,
            SegmentBackTranslation segmentBackTranslation,
            string segmentName,
            Stage stage) :
            base(urlPathSegment: "TabletTranscribeSegmentTranslate",
                viewModelContextProvider: viewModelContextProvider,
                pageName: pageName,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                nonDraftTranslationId: segmentBackTranslation.Id,
                secondPageName: AppResources.DoSegmentTranscribe)
        {
            SegmentBackTranslation = segmentBackTranslation;
            TranscribeTextBoxViewModel = transcribeTextBoxViewModel;

            var titleIconColor = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText") ?? new ColorReference();
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.Transcribe, titleIconColor)?.Glyph;

            var color = (ColorReference)ResourceExtensions.GetResourceValue("Option") ?? new ColorReference();
            var loopIcon = IconExtensions.BuildFontImageSource(Icon.Loop, color.Color, 30);
            var requireToListen = step.StepSettings.GetSetting(SettingType.RequireSegmentTranscribeListen);

            BarPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(
                audio: step.Role == Roles.Transcribe2 ? segmentBackTranslation.RetellBackTranslationAudio : segmentBackTranslation,
                actionState: requireToListen ? ActionState.Required : ActionState.Optional,
                title: segmentName,
                barPlayerPosition: 1,
                timeMarkers: null,
                passageMarkers: null,
                secondaryButtonIcon: loopIcon,
                secondaryButtonClickCommand: ReactiveCommand.CreateFromTask(LoopAudioAsync));

            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));

            ActionViewModelBaseSourceList.Add(TranscribeTextBoxViewModel);
            ActionViewModelBaseSourceList.Add(BarPlayerViewModel);

            //Listens for looping audio
            Disposables.Add(this.WhenAnyValue(
                    vm => vm.LoopAudio,
                    vm => vm.BarPlayerViewModel.AudioPlayerState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (!LoopAudio || BarPlayerViewModel.AudioPlayerState != AudioPlayerState.Loaded)
                    {
                        return;
                    }

                    BarPlayerViewModel.PlayAudioCommand.Execute().Subscribe();
                }));

            //Change the loop button background color when loop audio is set
            Disposables.Add(this.WhenAnyValue(
                    vm => vm.LoopAudio)
                .Subscribe(_ =>
                {
                    Color selectedColor;
                    if (!LoopAudio)
                    {
                        selectedColor = (ColorReference)ResourceExtensions.GetResourceValue("BarPlayerSecondaryStackBackground") ?? new ColorReference();
                        BarPlayerViewModel.SetSecondaryButtonBackgroundColor(selectedColor);
                        return;
                    }

                    selectedColor = (ColorReference)ResourceExtensions.GetResourceValue("RenderUserType") ?? new ColorReference();
                    BarPlayerViewModel.SetSecondaryButtonBackgroundColor(selectedColor);
                }));
        }

        private async Task<IRoutableViewModel> LoopAudioAsync()
        {
            LoopAudio = !LoopAudio;
            await Task.CompletedTask;
            return default;
        }

        protected async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            try
            {
                await SaveTranscriptionAsync();

                var viewModel = await TranscribeSegmentBackTranslateResolver.GetSegmentSelectPageViewModel(
                    Section,
                    Step,
                    ViewModelContextProvider,
                    SegmentBackTranslation);

                return await NavigateToAndReset(viewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private async Task SaveTranscriptionAsync()
        {
            var transcription = TranscribeTextBoxViewModel.Input;
            var segmentRepository = ViewModelContextProvider.GetSegmentBackTranslationRepository();
            var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
            if (Step.Role == Roles.Transcribe2)
            {
                SegmentBackTranslation.RetellBackTranslationAudio.SetTranscription(transcription);
                await retellRepository.SaveAsync(SegmentBackTranslation.RetellBackTranslationAudio);
            }
            else
            {
                SegmentBackTranslation.SetTranscription(transcription);
                await segmentRepository.SaveAsync(SegmentBackTranslation);
            }
        }

        public void PauseAudio()
        {
            if (BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing)
            {
                BarPlayerViewModel.Pause();
            }
        }

        public override void Dispose()
        {
            SegmentBackTranslation = null;
            BarPlayerViewModel?.Dispose();
            TranscribeTextBoxViewModel?.Dispose();

            base.Dispose();
        }
    }
}