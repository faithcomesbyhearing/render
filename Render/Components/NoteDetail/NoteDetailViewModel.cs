using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.AudioRecorder;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioServices;
using Render.Repositories.Extensions;
using Render.Components.Navigation;
using Render.Resources.Localization;
using DynamicData.Binding;

namespace Render.Components.NoteDetail
{
    public class NoteDetailViewModel : PopupViewModelBase<bool>
    {
        private readonly bool _selfCheck;
        private readonly Guid _loggedInUserId;

        private bool _creationMode;
        private IObservable<bool> _canPlayNoteAudio;
        private IDisposable _messageSubscription;

        [Reactive] public string Title { get; set; }
        [Reactive] public string TextMessage { get; set; }
        [Reactive] public InputState InputState { get; set; }
        [Reactive] public Conversation Conversation { get; set; }
        [Reactive] public bool RecordingIsInProgress { get; private set; }
        [Reactive] public bool NotePlayerPlayingIsInProgress { get; private set; }
        [Reactive] public IMiniAudioRecorderViewModel MiniAudioRecorderViewModel { get; set; }

        public DynamicDataWrapper<MessageContainerViewModel> Messages { get; private set; }

        public ReactiveCommand<Unit, Unit> EntryReturnCommand { get; }
        public ReactiveCommand<Unit, Unit> ForceCloseModalCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseModalCommand { get; }
        public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }
        public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }

        public ReactiveCommand<Conversation, Unit> InitCommand { get; private set; }
        public ReactiveCommand<Conversation, Unit> OnSaveCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> OnCloseCommand { get; private set; }
        public ReactiveCommand<Message, Unit> OnAddMessageCommand { get; private set; }
        public ReactiveCommand<Message, Unit> DeleteMessageCommand { get; private set; }

        public bool HasAutoCloseByBackgroundClick { get; private set; }
        public bool AllowEditing { get; set; }

        public ItemDetailNavigationViewModel NoteDetailNavigationViewModel { get; set; }

        public static async Task<NoteDetailViewModel> CreateAsync(bool creationMode,
            Conversation conversation,
            ReactiveCommand<Conversation, Unit> onSaveCommand,
            ReactiveCommand<Message, Unit> onDeleteMessageCommand,
            Guid projectId,
            IViewModelContextProvider viewModelContextProvider,
            bool selfCheck = false,
            bool allowEditing = true,
            ReactiveCommand<Unit, Unit> onCloseCommand = null,
            ReactiveCommand<Message, Unit> onAddMessageCommand = null)
        {
            var vm = new NoteDetailViewModel(creationMode,
                conversation,
                onSaveCommand,
                onDeleteMessageCommand,
                projectId,
                viewModelContextProvider,
                selfCheck,
                allowEditing,
                onCloseCommand,
                onAddMessageCommand);

            await vm.InitAsync(conversation);
            return vm;
        }

        private NoteDetailViewModel(bool creationMode,
            Conversation conversation,
            ReactiveCommand<Conversation, Unit> onSaveCommand,
            ReactiveCommand<Message, Unit> onDeleteMessageCommand,
            Guid projectId,
            IViewModelContextProvider viewModelContextProvider,
            bool selfCheck = false,
            bool allowEditing = true,
            ReactiveCommand<Unit, Unit> onCloseCommand = null,
            ReactiveCommand<Message, Unit> onAddMessageCommand = null)
            : base("NoteDetail", viewModelContextProvider)
        {
            _selfCheck = selfCheck;
            _loggedInUserId = GetLoggedInUserId();
            _creationMode = creationMode;
            _canPlayNoteAudio = this
                .WhenAnyValue(vm => vm.RecordingIsInProgress)
                .Select(isRecording => !isRecording);

            Title = AppResources.Notes;
            Conversation = conversation;
            TextMessage = string.Empty;
            HasAutoCloseByBackgroundClick = false;
            AllowEditing = allowEditing;
            Messages = new(SortExpressionComparer<MessageContainerViewModel>.Ascending(vm => vm.Message.DateUpdated));

            InitCommand = ReactiveCommand.CreateFromTask<Conversation>(InitAsync);
            EntryReturnCommand = ReactiveCommand.CreateFromTask(AddMessageAsync);

            ForceCloseModalCommand = ReactiveCommand.CreateFromTask(
                execute: ForceCloseConversationAsync,
                canExecute: EntryReturnCommand.IsExecuting.Select(x => !x));

            CloseModalCommand = ReactiveCommand.CreateFromTask(
                execute: CloseConversationAsync,
                canExecute: this
                    .WhenAnyValue(vm => vm.RecordingIsInProgress, vm => vm.TextMessage)
                    .Select(IsEditingInProgress));

            StartRecordingCommand = ReactiveCommand.Create(
                execute: StartRecording,
                canExecute: this
                    .WhenAnyValue(vm => vm.NotePlayerPlayingIsInProgress)
                    .Select(isPlaying => !isPlaying));

            StopRecordingCommand = ReactiveCommand.CreateFromTask(StopRecordingAsync);

            OnSaveCommand = onSaveCommand;
            DeleteMessageCommand = onDeleteMessageCommand;
            OnCloseCommand = onCloseCommand;
            OnAddMessageCommand = onAddMessageCommand;

            MiniAudioRecorderViewModel = viewModelContextProvider
                .GetMiniAudioRecorderViewModel(new Audio(default, projectId, Conversation.Id));

            Disposables.Add(this.WhenAnyValue(vm => vm.Conversation)
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .WhereNotNull()
                .InvokeCommand(InitCommand));

            Disposables.Add(this.WhenAnyValue(vm => vm.TextMessage)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => { InputState = x.Length > 0 ? InputState.Text : InputState.None; }));

            Disposables.Add(MiniAudioRecorderViewModel.WhenAnyValue(r => r.AudioRecorderState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(OnRecorderStateChanged));

            Disposables.Add(EntryReturnCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting));
            Disposables.Add(OnSaveCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting));
            Disposables.Add(StopRecordingCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting));

            static bool IsEditingInProgress((bool, string) stateMessage)
            {
                var isRecording = stateMessage.Item1;
                var message = stateMessage.Item2;

                return isRecording is false && string.IsNullOrEmpty(message);
            }
        }

        private async Task InitAsync(Conversation conversation)
        {
            if (conversation?.Messages == null)
            {
                CleanMessages();
                return;
            }

            var audioRepository = ViewModelContextProvider.GetAudioRepository();

            foreach (var message in conversation.Messages.Where(message => message.Media.Text.IsNullOrEmpty() && !message.Media.HasAudio))
            {
                message.Media.Audio = await audioRepository.GetByIdAsync(message.Media.AudioId);
            }

            var messages = new List<MessageContainerViewModel>();
            await Task.Run(async () =>
            {
                foreach (var message in conversation.Messages)
                {
                    messages.Add(await CreateMessageContainerViewModelAsync(message, AllowEditing));
                }
            });

            _messageSubscription?.Dispose();

            CleanMessages();    
            Messages.AddRange(messages);
            
            _messageSubscription = Messages.Observable
                .Publish()
                .AutoConnect()
                .WhenPropertyChanged(message => message.BarPlayerViewModel.AudioPlayerState)
                .WhereNotNull()
                .Subscribe(vm =>
                {
                    NotePlayerPlayingIsInProgress = Messages
                        .Items
                        .Where(message => message.HasAudio)
                        .Any(message => message.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing);
                });
        }

        public void SetNavigation(ItemDetailNavigationViewModel navigationViewModel)
        {
            NoteDetailNavigationViewModel = navigationViewModel;
            NoteDetailNavigationViewModel.OnBeforeChangeItemCommand = ReactiveCommand.CreateFromTask(
                async () => await PrepareToCloseConversationAsync());

            Disposables.Add(this.WhenAnyValue(x => x.NoteDetailNavigationViewModel.CurrentItem)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async conversation =>
                {
                    Conversation = conversation as Conversation;
                    await ResetInput();
                }));

            NoteDetailNavigationViewModel.MoveToNextItemCommand
                .IsExecuting
                .CombineLatest(InitCommand.IsExecuting)
                .Select((isExecutings) => isExecutings.First || isExecutings.Second)
                .Subscribe(isExecuting => IsLoading = isExecuting);

            NoteDetailNavigationViewModel.MoveToPreviousItemCommand
                .IsExecuting
                .CombineLatest(InitCommand.IsExecuting)
                .Select((isExecutings) => isExecutings.First || isExecutings.Second)
                .Subscribe(isExecuting => IsLoading = isExecuting);
        }

        private async Task AddMessageAsync()
        {
            var audio = await MiniAudioRecorderViewModel.GetAudio();

            if (audio.HasAudio || !string.IsNullOrEmpty(TextMessage))
            {
                var media = audio.HasAudio
                    ? new Media(audio.Id) { Audio = audio }
                    : new Media(text: TextMessage);

                var newMessage = new Message(GetLoggedInUserId(), media);

                Conversation.Messages.Add(newMessage);

                var message = await CreateMessageContainerViewModelAsync(newMessage, AllowEditing);

                Messages.Add(message);

                if (newMessage.Media.HasAudio)
                {
                    newMessage.Media.Audio.SetAudio(newMessage.Media.Audio.Data);
                    var audioRepository = ViewModelContextProvider.GetAudioRepository();

                    await audioRepository.SaveAsync(newMessage.Media.Audio);
                }

                await OnSaveCommand.Execute(Conversation);
                if (OnAddMessageCommand != null)
                {
                    await OnAddMessageCommand.Execute(newMessage);
                }
            }

            await ResetInput();
        }

        private async Task DeleteMarkedMessagesAsync()
        {
            var deletedMessages = Messages.Items.Where(message => message.IsDeleted).ToList();

            foreach(var deletedMessage in deletedMessages)
            {
                if (deletedMessage.HasAudio)
                {
                    var audioRepository = ViewModelContextProvider.GetAudioRepository();
                    await audioRepository.DeleteAudioByIdAsync(deletedMessage.Message.Media.AudioId);
                }

                Messages.Remove(deletedMessage);

                deletedMessage.Dispose();

                await DeleteMessageCommand.Execute(deletedMessage.Message);
            }
        }

        private async Task ResetInput()
        {
            TextMessage = string.Empty;
            InputState = InputState.None;
            var audio = await MiniAudioRecorderViewModel.GetAudio();
            if (audio.HasAudio)
            {
                MiniAudioRecorderViewModel.SetAudio(new Audio(audio.ScopeId, audio.ProjectId, Conversation.Id));
            }
        }

        private void StartRecording()
        {
            RecordingIsInProgress = true;
            MiniAudioRecorderViewModel.StartRecordingCommand.Execute().Subscribe();
        }

        private async Task StopRecordingAsync()
        {
            await MiniAudioRecorderViewModel.StopRecordingCommand.Execute();
            await AddMessageAsync();
        }

        private async Task PrepareToCloseConversationAsync(bool? creationMode = null)
        {
            _creationMode = creationMode ?? _creationMode;

            await MiniAudioRecorderViewModel.StopRecorderActivity();

            //If we have no messages, clear the audio out of the recorder
            //so the conversation gets deleted
            if (Conversation.Messages.Count == 0)
            {
                MiniAudioRecorderViewModel.DeleteCommand.Execute().Subscribe();
            }

            PauseCurrentMessage();

            await DeleteMarkedMessagesAsync();

            if (!_creationMode) //Skip if we are in creation mode. In creation mode Messages saved in AddMessageAsync already
            {
                var anyMessagesChanged = false;

                foreach (var message in Conversation.Messages)
                {
                    //Otherwise check if the user has been added to the list already, if yes no change
                    //add user id to previously seen list
                    var messageSeenStatus = message.GetSeenStatus(_loggedInUserId);
                    if (messageSeenStatus) continue;
                    if (message.UserId != _loggedInUserId || _selfCheck)
                    {
                        message.AddUserIdToSeenList(_loggedInUserId);
                        anyMessagesChanged = true;
                    }
                }

                if (anyMessagesChanged)
                {
                    await OnSaveCommand.Execute(Conversation);
                }
            }
        }

        private async Task CloseConversationAsync()
        {
            await PrepareToCloseConversationAsync();

            if (OnCloseCommand != null)
            {
                await OnCloseCommand.Execute();
            }

            ClosePopup(Conversation.Messages.Any());
        }

        private async Task ForceCloseConversationAsync()
        {
            await MiniAudioRecorderViewModel.StopRecordingCommand.Execute();
            await CloseConversationAsync();
        }

        private async Task<MessageContainerViewModel> CreateMessageContainerViewModelAsync(Message newMessage, bool allowEditing)
        {
            var messageContainerViewModel = await MessageContainerViewModel.CreateAsync(
                message: newMessage,
                allowEditing: allowEditing,
                viewModelContextProvider: ViewModelContextProvider,
                canPlay: _canPlayNoteAudio);

            return messageContainerViewModel;
        }

        private void OnRecorderStateChanged(AudioRecorderState state)
        {
            if (state == AudioRecorderState.NoAudio)
			{
				RecordingIsInProgress = false;
			}
        }

        private void PauseCurrentMessage(Guid? excludedId = null)
        {
            var currentMessage = Messages.Items.FirstOrDefault(m => m.BarPlayerViewModel != null
                                                                    && m.BarPlayerViewModel.AudioPlayerState == AudioPlayerState.Playing
                                                                    && m.MessageId != excludedId);

            currentMessage?.BarPlayerViewModel.PauseAudioCommand.Execute().Subscribe();
        }

        private void CleanMessages()
        {
            Messages.Items.DisposeCollection();
            Messages.Clear();
        }

        public override void Dispose()
        {
            _messageSubscription?.Dispose();

            Messages?.Dispose();
            MiniAudioRecorderViewModel?.Dispose();
            NoteDetailNavigationViewModel?.Dispose();

            base.Dispose();
        }
    }

    public enum InputState
    {
        Text,
        Audio,
        None
    }
}