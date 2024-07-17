using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Resources.Localization;
using Render.Services.AudioServices;

namespace Render.Components.NoteDetail
{
    public class MessageContainerViewModel : ActionViewModelBase
    {
        public string Text { get; set; }
        public IBarPlayerViewModel BarPlayerViewModel { get; }
        public IBarPlayerViewModel InterpretBarPlayerViewModel { get; }

        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string Author { get; set; }
       
        [Reactive]
        public bool AllowDelete { get; set; }
        [Reactive]
        public bool HasAudio { get; private set; }
        [Reactive] 
        public bool HasInterpretedAudio { get; private set; }
        [Reactive]
        public bool IsAuthor { get; private set; }
        
        public Message Message { get; }
        public ReactiveCommand<Unit, Unit> OnDeleteCommand { get; } //command called by click on trash can
        private readonly ReactiveCommand<MessageContainerViewModel, Unit> _onDeleteMessageCommand;

        public static async Task<MessageContainerViewModel> CreateAsync(
            Message message, 
            bool allowEditing, 
            ReactiveCommand<MessageContainerViewModel, Unit> onDeleteMessageCommand, 
            IViewModelContextProvider viewModelContextProvider,
            IObservable<bool> canPlay=null)
        {
            var loggedInUser = viewModelContextProvider.GetLoggedInUser();
            var isLoggedInUser = loggedInUser.Id == message.UserId;
            var author = loggedInUser.FullName;
            var interpreterName = string.Empty;
            var userRepository = viewModelContextProvider.GetUserRepository();
            if(!isLoggedInUser)
            {
                var createdByUser = await userRepository.GetUserAsync(message.UserId);
                author = createdByUser is null ? AppResources.DeletedUser : createdByUser.FullName;
            }
            
            if (message.InterpreterUserId != Guid.Empty)
            {
                var interpreterUser = await userRepository.GetUserAsync(message.InterpreterUserId);
                interpreterName = interpreterUser is null ? AppResources.DeletedUser : interpreterUser.FullName;
            }
            
            var vm = new MessageContainerViewModel(message, author, onDeleteMessageCommand, viewModelContextProvider, interpreterName, canPlay)
            {
                AllowDelete = isLoggedInUser && allowEditing,
                IsAuthor = isLoggedInUser
            };

            return vm;
        }

        protected MessageContainerViewModel(
            Message message, 
            string author, 
            ReactiveCommand<MessageContainerViewModel, Unit> onDeleteMessageCommand, 
            IViewModelContextProvider viewModelContextProvider,
            string interpreterName,
            IObservable<bool> canPlay=null) :
            base(ActionState.Optional, "", viewModelContextProvider)
        {
            MessageId = message.Id;
            UserId = message.UserId;
            Text = message.Media.Text;
            Author = author;
            Message = message;
            _onDeleteMessageCommand = onDeleteMessageCommand;
            OnDeleteCommand = ReactiveCommand.CreateFromTask(DeleteMessageAsync);
            if (message.Media.HasAudio)
            {
                HasAudio = true;
            }
            
            //If no text, must be an audio note
            if (string.IsNullOrEmpty(message.Media.Text))
            {
                HasAudio = true;

                var interpretedAudio = message.InterpretationAudio;
                HasInterpretedAudio = interpretedAudio != null;

                var barPlayerTitle = string.Format($"{author} - {AppResources.Original}");
                BarPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(
                    media: message.Media, 
                    actionState: ActionState.Optional, 
                    title: HasInterpretedAudio ? barPlayerTitle : author,
                    canPlayAudio: canPlay);

                if (HasInterpretedAudio)
                {
                    var interpretPlayerTitle = string.Format($"{interpreterName} - {AppResources.Interpreted}");
                    InterpretBarPlayerViewModel = viewModelContextProvider.GetBarPlayerViewModel(
                        audio: message.InterpretationAudio,
                        actionState: ActionState.Optional,
                        title: interpretPlayerTitle,
                        barPlayerPosition: 0,
                        canPlayAudio: canPlay);

                    var players = new SourceList<IBarPlayerViewModel>();
                    players.AddRange(new [] { BarPlayerViewModel, InterpretBarPlayerViewModel });

                    Disposables.Add(players
                        .Connect()
                        .Publish()
                        .AutoConnect()
                        .WhenPropertyChanged(player => player.AudioPlayerState)
                        .Subscribe((propertyValue) =>
                        {
                            if (propertyValue.Value is AudioPlayerState.Playing) 
                            {
                                players.Items
                                    .Where(player => player != null && player != propertyValue.Sender)
                                    .ForEach(player => player.Pause());
                            }
                        }));
                }
            }
        }

        private async Task DeleteMessageAsync()
        {
            //Tell parent vm note detail that this message is deleted
            await _onDeleteMessageCommand.Execute(this);
        }
        
        public override void Dispose()
        {
            BarPlayerViewModel?.Dispose();
            InterpretBarPlayerViewModel?.Dispose();

            base.Dispose();
        }
    }
}