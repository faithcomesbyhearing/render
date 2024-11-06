using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.TranscribeTextBox;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;
using Render.Services.AudioServices;

namespace Render.Pages.Transcribe.TranscribeRetellBackTranslate
{
    public class TranscribeRetellPassageTranslatePageViewModel : WorkflowPageBaseViewModel
    {
        private string PassageTitle { get; }
        private RetellBackTranslation RetellBackTranslation { get; set; }
        [Reactive] public TranscribeTextBoxViewModel TranscribeTextBoxViewModel { get; private set; }
        [Reactive] public bool LoopAudio { get; set; }
        public IBarPlayerViewModel BarPlayerViewModel { get; private set; }

        public static async Task<TranscribeRetellPassageTranslatePageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            Stage stage)
        {
            var transcriptionText = step.Role == Roles.Transcribe2 ? 
                retellBackTranslation.RetellBackTranslationAudio.Transcription ?? string.Empty : 
                retellBackTranslation.Transcription ?? string.Empty;

            var transcriptionWindowViewModel = await TranscribeTextBoxViewModel.CreateAsync(viewModelContextProvider, 
                transcriptionText);
                
            var pageViewModel = new TranscribeRetellPassageTranslatePageViewModel(
                viewModelContextProvider,
                step,
                section,
                passage,
                retellBackTranslation,
                transcriptionWindowViewModel,
                stage);
           
            pageViewModel.TranscribeTextBoxViewModel = transcriptionWindowViewModel;
                
            return pageViewModel;
        }

        private TranscribeRetellPassageTranslatePageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Step step,
            Section section,
            Passage passage,
            RetellBackTranslation retellBackTranslation,
            TranscribeTextBoxViewModel transcribeTextBoxViewModel,
            Stage stage) :
            base(
                urlPathSegment: "TabletTranscribeRetellBTPassageTranslate",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                secondPageName: AppResources.DoPassageTranscribe)
        {
            
            RetellBackTranslation = retellBackTranslation;
            TranscribeTextBoxViewModel = transcribeTextBoxViewModel;
            
            PassageTitle = string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString);
            
            var titleIconColor = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText") ??
                                 new ColorReference();
            
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.Transcribe, titleIconColor)?.Glyph;
            
            var requireToListen = step.StepSettings.GetSetting(SettingType.RequirePassageTranscribeListen);
            
            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            
            var loopIconColor = (ColorReference)ResourceExtensions.GetResourceValue("Option") ?? new ColorReference();
            var loopIcon = IconExtensions.BuildFontImageSource(Icon.Loop, loopIconColor.Color, 30);
            
            BarPlayerViewModel =
                viewModelContextProvider.GetBarPlayerViewModel(
                    step.Role == Roles.Transcribe2
                        ? retellBackTranslation?.RetellBackTranslationAudio
                        : retellBackTranslation,
                    requireToListen ? ActionState.Required : ActionState.Optional,
                    PassageTitle, 1, null, null,
                    loopIcon,
                    ReactiveCommand.CreateFromTask(LoopAudioAsync));

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
                        selectedColor =
                            (ColorReference)ResourceExtensions.GetResourceValue("BarPlayerSecondaryStackBackground") ??
                            new ColorReference();
                        
                        BarPlayerViewModel.SetSecondaryButtonBackgroundColor(selectedColor);
                        
                        return;
                    }

                    selectedColor = (ColorReference)ResourceExtensions.GetResourceValue("RenderUserType") ??
                                    new ColorReference();
                    
                    BarPlayerViewModel.SetSecondaryButtonBackgroundColor(selectedColor);
                }));
        }
        
        private async Task SaveTranscriptionAsync()
        {
            var transcription = TranscribeTextBoxViewModel.Input;
            
            var retellRepository = ViewModelContextProvider.GetRetellBackTranslationRepository();
            
            var retell = Step.Role == Roles.Transcribe2
                ? RetellBackTranslation.RetellBackTranslationAudio
                : RetellBackTranslation;
            
            retell.SetTranscription(transcription);
            
            await retellRepository.SaveAsync(retell);
        }
        private async Task<IRoutableViewModel> LoopAudioAsync()
        {
            LoopAudio = !LoopAudio;
            await Task.CompletedTask;
            return default;
        }
        
        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            try
            {
                await SaveTranscriptionAsync();
                
                var viewModel = await TranscribeRetellBackTranslateResolver.GetTranscribeRetellPassageSelectViewModelAsync(
                    Section, Step, ViewModelContextProvider);
                
                return await NavigateToAndReset(viewModel);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
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
            RetellBackTranslation = null;
            
            BarPlayerViewModel?.Dispose();
            BarPlayerViewModel = null;
            
            TranscribeTextBoxViewModel?.Dispose();
            TranscribeTextBoxViewModel = null;

            base.Dispose();
        }
    }
}