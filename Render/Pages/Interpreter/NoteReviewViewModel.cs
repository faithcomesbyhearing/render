using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.Interpreter
{
    public class NoteReviewViewModel : WorkflowPageBaseViewModel
    {
        private readonly Dictionary<Draft, List<Message>> _messageDictionary;
        public readonly IBarPlayerViewModel OriginalNotePlayer;
        public readonly IBarPlayerViewModel InterpretedNotePlayer;
        
        private Draft Draft { get; set; }
        private Message Message { get; set; }
        public ReactiveCommand<Unit,IRoutableViewModel> ReRecordNoteCommand { get; }
        
        public static NoteReviewViewModel Create(
            Step step,
            Section section,
            Draft draft,
            Message message,
            Stage stage,
            Dictionary<Draft, List<Message>> messageDictionary,
            IViewModelContextProvider viewModelContextProvider)
        {
            var pageName = AppResources.Note + " ";
            pageName += step.RenderStepType == RenderStepTypes.InterpretToConsultant
                ? AppResources.InterpretToConsultant
                : AppResources.InterpretToTranslator;
            
            return new NoteReviewViewModel(
                step,
                section,
                draft,
                message,
                stage,
                messageDictionary,
                viewModelContextProvider,
                pageName);
        }

        private NoteReviewViewModel(
            Step step,
            Section section,
            Draft draft,
            Message message,
            Stage stage,
            Dictionary<Draft, List<Message>> messageDictionary,
            IViewModelContextProvider viewModelContextProvider,
            string pageName) : 
            base("NoteReview", viewModelContextProvider, pageName, section, stage, step,
                nonDraftTranslationId: message.Id, secondPageName: AppResources.DoNoteReview)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;
            TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.NoteTranslate)?.Glyph;
            
            Draft = draft;
            Message = message;
            _messageDictionary = messageDictionary;
            
            var interpretedNoteAudio = message.InterpretationAudio;
            var requireNoteListen = step.StepSettings.GetSetting(SettingType.RequireNoteReview);
            
            OriginalNotePlayer = new BarPlayerViewModel(message.Media, viewModelContextProvider, 
                    requireNoteListen ? ActionState.Required : ActionState.Optional, AppResources.Note);
            
            var btMultiStep = stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
                .First(x => x.Role == Roles.BackTranslate);
            string consultantLanguage;
            // use first step consultant language for interpretToTranslator
            if (step.RenderStepType == RenderStepTypes.InterpretToTranslator)
            {
                var bt1stStep = btMultiStep.GetSubSteps()
                    .First(x => x.RenderStepType == RenderStepTypes.BackTranslate);
                consultantLanguage = bt1stStep.StepSettings
                    .GetString(bt1stStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate) 
                        ? SettingType.SegmentConsultantLanguage : SettingType.ConsultantLanguage);
            }
            // use second step consultant language for interpretToTConsultant
            else
            {
                var bt2ndbtStep = btMultiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                    .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                    .First(x => x.Role == Roles.BackTranslate2);
                consultantLanguage = bt2ndbtStep.StepSettings
                    .GetString(bt2ndbtStep.StepSettings.GetSetting(SettingType.DoSegmentBackTranslate) 
                        ? SettingType.SegmentConsultant2StepLanguage : SettingType.Consultant2StepLanguage);
            }
            
            InterpretedNotePlayer = viewModelContextProvider.GetBarPlayerViewModel(interpretedNoteAudio, 
                    requireNoteListen ? ActionState.Required : ActionState.Optional, 
                    string.IsNullOrEmpty(consultantLanguage) ? AppResources.Translation 
                        : $"{AppResources.Translation} - {consultantLanguage}", 0);
            
            ActionViewModelBaseSourceList.AddRange(new[] { OriginalNotePlayer, InterpretedNotePlayer });

            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));
            ReRecordNoteCommand = ReactiveCommand.CreateFromTask(NavigateToNoteInterpret);
            Disposables.Add(ReRecordNoteCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));
            SetProceedButtonIcon();
        }
        
        protected sealed override void SetProceedButtonIcon()
        {
            var tuple = NoteInterpretViewModel.FindNextMessageWithoutInterpretation(_messageDictionary);
            if (tuple == default)
            {
                ProceedButtonViewModel.IsCheckMarkIcon = true;
            }
        }

        private async Task<IRoutableViewModel> NavigateToNoteInterpret()
        {
            var noteInterpretViewModel = await Task.Run(() => NoteInterpretViewModel.Create(Step, Section, Draft, Message,
                Stage, _messageDictionary, ViewModelContextProvider));

            return await NavigateToAndReset(noteInterpretViewModel);
        }
        
        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            await SaveDraftAsync(Draft, Message, ViewModelContextProvider);
            
            var tuple = NoteInterpretViewModel.FindNextMessageNeedingInterpretation(_messageDictionary);
            if (tuple != default)
            {
                var viewModel = await Task.Run(async () =>
                {
                    return NoteInterpretViewModel.Create(Step, Section, tuple.draft, tuple.message,
                        Stage, _messageDictionary, ViewModelContextProvider);
                });
                return await NavigateTo(viewModel);
            }
            await Task.Run(async () =>
            {
                await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);
            });
            return await NavigateToHomeOnMainStackAsync();
        }

        public static async Task SaveDraftAsync(Draft draft, Message message, IViewModelContextProvider viewModelContextProvider)
        {
            message.CompleteInterpretation();
            if (draft is SegmentBackTranslation segment)
            {
                var segmentRepository = viewModelContextProvider.GetSegmentBackTranslationRepository();
                await segmentRepository.SaveAsync(segment);
            }
            else if (draft is RetellBackTranslation retell)
            {
                var retellRepository = viewModelContextProvider.GetRetellBackTranslationRepository();
                await retellRepository.SaveAsync(retell);
            }
            else
            {
                var draftRepository = viewModelContextProvider.GetDraftRepository();
                await draftRepository.SaveAsync(draft);
            }
        }
        
        public override void Dispose()
        {
            Draft = null;
            Message = null;

            OriginalNotePlayer?.Dispose();
            InterpretedNotePlayer?.Dispose();

            base.Dispose();
        }
    }
}