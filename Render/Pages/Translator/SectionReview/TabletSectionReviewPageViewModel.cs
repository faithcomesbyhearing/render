using DynamicData;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Sequencer;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using System.Reactive;
using System.Reactive.Linq;
using Render.Components.NoteDetail;

namespace Render.Pages.Translator.SectionReview
{
    public class TabletSectionReviewPageViewModel : SectionReviewPageBaseViewModel
    {
        private Dictionary<Guid, SequencerNoteDetailViewModel> PassageSequencerNoteDetailViewModels { get; set; } = new();
        public DynamicDataWrapper<IBarPlayerViewModel> References { get; private set; }
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }
        private ActionViewModelBase SequencerActionViewModel { get; set; }
        private ReactiveCommand<Unit, Unit> NavigateToDraftingCommand { get; set; }

        public static async Task<TabletSectionReviewPageViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage)
        {
            // WorkflowPageBaseViewModel base constructor initializes SessionStateService and should be invoked at the beginning
            var pageVm = new TabletSectionReviewPageViewModel(viewModelContextProvider, section, step, stage);

            pageVm.Initialize();
            return pageVm;
        }
        
        private TabletSectionReviewPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage) :
            base(viewModelContextProvider, section, step, "TabletSectionReviewPage", stage)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;
            
            References = new DynamicDataWrapper<IBarPlayerViewModel>();
        }

        private void Initialize()
        {
            // Toolbar
            NavigateToDraftingCommand = ReactiveCommand.CreateFromTask(NavigateToDraftingAsync);
            NavigateToDraftingCommand
                .IsExecuting
                .Subscribe(isExecuting => IsLoading = isExecuting);

            PopulateReferences();

            SetupSequencer();
        }

        private void PopulateReferences()
        {
            var isRequired = Step.StepSettings.GetSetting(SettingType.RequireSectionReview);
            
            //Initialize bar players
            var referenceCount = 0;

            foreach (var sectionReferenceAudio in Section.References)
            {
                var vm = ViewModelContextProvider.GetBarPlayerViewModel(sectionReferenceAudio,
                    isRequired ? ActionState.Required : ActionState.Optional,
                    sectionReferenceAudio.Reference.Name, referenceCount++);

                References.Add(vm);
                ActionViewModelBaseSourceList.Add(vm);
            }
        }
        
        private void SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer, FlagType.Note);

            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            var audios = Section.Passages.Select(passage => passage
                .CreatePlayerAudioModel(ViewModelContextProvider
                    .GetTempAudioService(passage.CurrentDraftAudio)
                    .SaveTempAudio(), FlagType.Note)).ToArray();

            SequencerPlayerViewModel.SetAudio(audios);
            
            SequencerPlayerViewModel.AddToolbarItem(new ToolbarItemModel(ToolbarItemType.Custom, "ReRecord", NavigateToDraftingCommand), 0);
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireSectionReview),
                requirementId: Section.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            Disposables.Add(SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing)
                .Subscribe(_ => {
                    SequencerActionViewModel.ActionState = ActionState.Optional;
                }));

            SequencerPlayerViewModel.AddFlagCommand = ReactiveCommand.CreateFromTask<IFlag, bool>(ShowConversationAsync);
            SequencerPlayerViewModel.TapFlagCommand = ReactiveCommand.CreateFromTask(
                async (IFlag flag) => { await ShowConversationAsync(flag); });
            
            InitializeNoteDetails();
        }

        private void InitializeNoteDetails()
        {
            foreach (var passage in Section.Passages)
            {
                var sequencerNoteDetailViewModel = new SequencerNoteDetailViewModel(
                    passage.CurrentDraftAudio.Conversations,
                    Section,
                    Stage,
                    SequencerPlayerViewModel,
                    ViewModelContextProvider,
                    ActionViewModelBaseSourceList,
                    Disposables);
                
                sequencerNoteDetailViewModel.SaveCommand = ReactiveCommand.CreateFromTask<Conversation>(SaveDraftWithNoteAsync);
                sequencerNoteDetailViewModel.DeleteMessageCommand = ReactiveCommand.CreateFromTask<Message>(DeleteMessageAsync);

                sequencerNoteDetailViewModel.AddMessageCommand = ReactiveCommand.Create((Message _) =>
                {
                    ViewModelContextProvider.GetGrandCentralStation().SetHasNewMessageForWorkflowStep(Section, Step, true);
                });

                PassageSequencerNoteDetailViewModels.Add(passage.Id, sequencerNoteDetailViewModel);
            }
        }

        private async Task NavigateToDraftingAsync()
        {
            var audio = SequencerPlayerViewModel.GetCurrentAudio();

            if (audio is null)
            {
                return;
            }

            var passage = Section.Passages.FirstOrDefault(passage => passage.Id == audio.Key);

            if (passage == null)
            {
                return;
            }

            await NavigateToDraftingAsync(passage);
        }

        private async Task<bool> ShowConversationAsync(IFlag flag)
        {
            if (flag.Key == default && SequencerPlayerViewModel?.GetCurrentAudio() != null)
            {
                return await PassageSequencerNoteDetailViewModels[SequencerPlayerViewModel.GetCurrentAudio().Key].ShowConversationAsync(flag);
            }

            var flagPassages = PassageSequencerNoteDetailViewModels.Where(ps => ps.Value.Conversations.Any(c => c.Id == flag.Key));

            if (!flagPassages.Any())
            {
                return false;
            }

            return await flagPassages.First().Value.ShowConversationAsync(flag);
        }

        private async Task SaveDraftWithNoteAsync(Conversation conversation)
        {
            var audio = SequencerPlayerViewModel.GetCurrentAudio();

            var currentPassage = Section.Passages.Single(p => p.Id == audio.Key);
            
            currentPassage.CurrentDraftAudio.UpdateOrDeleteConversation(conversation);
            
            await ViewModelContextProvider.GetDraftRepository().SaveAsync(currentPassage.CurrentDraftAudio);
        }

        private async Task DeleteMessageAsync(Message message)
        {
            var currentPassage =
                Section.Passages.Single(p =>
                    p.CurrentDraftAudio.Conversations.Any(c => c.Messages.Any(m => m.Id == message.Id)));
            
            foreach (var conversation in currentPassage.CurrentDraftAudio.Conversations)
            {
                var removed = conversation.Messages.Remove(message);
                
                if (removed)
                {
                    await SaveDraftWithNoteAsync(conversation);
                    break;
                }
            }
        }

        public override void Dispose()
        {
            References.Dispose();

            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;
            PassageSequencerNoteDetailViewModels.Values.DisposeCollection();
            PassageSequencerNoteDetailViewModels = null;
            
            ActionViewModelBaseSourceList?.Dispose();
            
            base.Dispose();
        }
    }
}