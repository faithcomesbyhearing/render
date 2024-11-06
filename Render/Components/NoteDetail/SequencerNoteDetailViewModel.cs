using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.Navigation;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow.Stage;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.TempFromVessel.Kernel;
using System.Reactive;
using System.Reactive.Linq;

namespace Render.Components.NoteDetail
{
    /// <summary>
    /// Proxy class to setup relation between Sequencer and NoteDetail popup
    /// </summary>
    public class SequencerNoteDetailViewModel : IDisposable
    {
        private readonly bool _conversationMarkersRequired;
        private readonly bool _isSelfCheck;
        private readonly bool _allowEditing;
        public IEnumerable<Conversation> Conversations { get; private set; }
        public ISequencerViewModel SequencerViewModel { get; private set; }
        public DynamicDataWrapper<ConversationViewModel> ConversationMarkers { get; private set; }
        public Stage Stage { get; private set; }
        public Section Section { get; private set; }
        public IViewModelContextProvider ViewModelContextProvider { get; private set; }
        public SourceList<IActionViewModelBase> ActionViewModelBaseSourceList { get; private set; }
        public List<IDisposable> Disposables { get; private set; }

        [Reactive]
        public ReactiveCommand<Conversation, Unit> SaveCommand { get; set; }

        [Reactive]
        public ReactiveCommand<Message, Unit> AddMessageCommand { get; set; }

        [Reactive]
        public ReactiveCommand<Message, Unit> DeleteMessageCommand { get; set; }

        private readonly Predicate<Conversation> _conversationsFilter;

        public SequencerNoteDetailViewModel(
            IEnumerable<Conversation> conversations, 
            Section section, 
            Stage stage, 
            ISequencerViewModel sequencerViewModel, 
            IViewModelContextProvider provider,
            SourceList<IActionViewModelBase> actionList,
            List<IDisposable> disposables,
            bool conversationMarkersRequired = false,
            bool isSelfCheck = false,
            bool allowEditing = true,
            Predicate<Conversation> conversationsFilter = null)
        {
            Stage = stage;
            Section = section;
            Conversations = conversations;
            SequencerViewModel = sequencerViewModel;
            _conversationMarkersRequired = conversationMarkersRequired;
            _isSelfCheck = isSelfCheck;
            _allowEditing = allowEditing;
            ViewModelContextProvider = provider;
            ActionViewModelBaseSourceList = actionList;
            Disposables = disposables;
            _conversationsFilter = conversationsFilter;

            ConversationMarkers = new DynamicDataWrapper<ConversationViewModel>(SortExpressionComparer<ConversationViewModel>
                .Ascending(conversation => conversation.TimeMarker));

            SetupConversationMarkers();
        }

        public async Task<bool> ShowConversationAsync(IFlag flag)
        {
            var conversationCreationMode = flag.Key == default;

            var conversation = conversationCreationMode ?
                new Conversation(flag.PositionSec, Stage.Id) :
                Conversations.First(conversation => conversation.Id == flag.Key);

            if (conversationCreationMode)
            {
                flag.Key = conversation.Id;
            }

            var noteDetailViewModel = await NoteDetailViewModel.CreateAsync(
                creationMode: conversationCreationMode,
                conversation: conversation,
                onSaveCommand: SaveCommand,
                onAddMessageCommand:  ReactiveCommand.CreateFromTask<Message>(AddMessageAsync),
                onDeleteMessageCommand: ReactiveCommand.CreateFromTask<Message>(DeleteMessageAsync),
                projectId: Section.ProjectId,
                viewModelContextProvider: ViewModelContextProvider,
                selfCheck: _isSelfCheck,
                allowEditing: _allowEditing);

            var navigationViewModel = new ItemDetailNavigationViewModel(
                item: conversation,
                markers: ConversationMarkers.Items,
                onChangeItemCommand: ReactiveCommand.Create<DomainEntity>(entity => SetMarkerAsViewed(entity.Id)),
                viewModelContextProvider: ViewModelContextProvider,
                canMove: noteDetailViewModel
                    .WhenAnyValue(noteVm => noteVm.RecordingIsInProgress)
                    .Select(isRecording => !isRecording));

            noteDetailViewModel.SetNavigation(navigationViewModel);
            SetMarkerAsViewed(conversation.Id);

            await noteDetailViewModel.ShowPopupAsync();
            noteDetailViewModel.Dispose();

            return conversation.Messages.Any();
        }
        
        private async Task AddMessageAsync(Message message)
        {
            if(AddMessageCommand is not null)
            {
                await AddMessageCommand.Execute(message);
            }
            
            var conversation = Conversations.FirstOrDefault(c => c.Messages.Contains(message));
            UpdateConversationMarkers(conversation);
        }

        private async Task DeleteMessageAsync(Message message)
        {   
            //Message can be deleted in the command below, previously need to find conversation
            var conversation = Conversations.FirstOrDefault(c => c.Messages.Contains(message));

            if (DeleteMessageCommand is not null)
            {
                await DeleteMessageCommand.Execute(message);
            }

            UpdateConversationMarkers(conversation);
        }

        private void SetupConversationMarkers()
        {
            if (SequencerViewModel is null)
            {
                return;
            }
            
            var filteredConversations = _conversationsFilter is null ? Conversations : Conversations.Where(c => _conversationsFilter(c));
            foreach (var conversation in filteredConversations)
            {
                var marker = SequencerViewModel.CreateConversationMarker(conversation, ViewModelContextProvider, Disposables, _conversationMarkersRequired);

                ConversationMarkers.Add(marker);
                ActionViewModelBaseSourceList.Add(marker);
            }
        }

        private void UpdateConversationMarkers(Conversation conversation)
        {
            if (SequencerViewModel is null)
            {
                return;
            }
            
            if (conversation.Messages.Any())
            {
                AddConversationMarker(conversation);
            }
            else
            {
                RemoveConversationMarker(conversation);
            }
        }

        private void AddConversationMarker(Conversation conversation)
        {
            var marker = ConversationMarkers.Items.FirstOrDefault(cm => cm.Conversation.Id == conversation.Id);

            if (marker is null)
            {
                marker = SequencerViewModel.CreateConversationMarker(conversation, ViewModelContextProvider, Disposables, _conversationMarkersRequired);
                ConversationMarkers.Add(marker);
                ActionViewModelBaseSourceList.Add(marker);
            }

            var flag = SequencerViewModel.GetFlag(conversation.Id);
            if (flag is null)
            {
                SequencerViewModel.AddFlag(new NoteFlagModel(conversation.Id, conversation.Flag, false, true));
            }
        }

        private void RemoveConversationMarker(Conversation conversation)
        {
            var marker = ConversationMarkers.Items.FirstOrDefault(cm => cm.Conversation.Id == conversation.Id);

            if (marker is not null)
            {
                ConversationMarkers.Remove(marker);
                ActionViewModelBaseSourceList.Remove(marker);
            }

            var flag = SequencerViewModel.GetFlag(conversation.Id);
            if (flag is not null)
            {
                SequencerViewModel.RemoveFlag(flag);
            }
        }

        private void SetMarkerAsViewed(Guid conversationId)
        {
            var marker = ConversationMarkers.SourceItems.FirstOrDefault(cm => cm.Conversation.Id == conversationId);

            if (marker is null)
            {
                return;
            }

            marker.FlagState = NotePlacementPlayer.FlagState.Viewed;
        }
        
        public void Dispose()
        {
            Stage = null;
            Section = null;
            
            ConversationMarkers?.Dispose();
        }
    }
}
