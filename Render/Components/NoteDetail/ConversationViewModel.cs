﻿using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;
using Render.Components.NotePlacementPlayer;
using Render.Components.Navigation;
using Render.TempFromVessel.Kernel;
using ReactiveUI;
using System.Reactive.Linq;

namespace Render.Components.NoteDetail
{
    //TODO: Should be renamed to 'ConversationMarkerViewModel', existing ConversationMarkerViewModel is deprecated and need to be removed
    public class ConversationViewModel : ActionViewModelBase, INavigationMarker
    {
        [Reactive]
        public Conversation Conversation { get; private set; }

        [Reactive]
        public FlagState FlagState { get; set; }

        [Reactive]
        public double TimeMarker { get; set; }

        public DomainEntity Item => Conversation;

        public ConversationViewModel(Conversation conversation, 
            IViewModelContextProvider viewModelContextProvider,
            bool needToSetRequiredFlagStateInsteadOptional = false)
            : base(ActionState.Optional, "ConversationMarker", viewModelContextProvider)
        {
            Conversation = conversation;
            FlagState = GetFlagState(needToSetRequiredFlagStateInsteadOptional);
            TimeMarker = conversation.FlagOverride;

            Disposables.Add(this.WhenAnyValue(vm => vm.FlagState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    ActionState = FlagState == FlagState.Required ? ActionState.Required : ActionState.Optional;
                }));
        }

        private FlagState GetFlagState(bool required)
        {
            var isSeen = true;
            var loggedInUserId = ViewModelContextProvider.GetLoggedInUser().Id;
            foreach (var message in Conversation.Messages)
            {
                if (message.GetSeenStatus(loggedInUserId) == false && message.UserId != loggedInUserId)
                {
                    isSeen = false;
                    break;
                }
            }

            return isSeen ? FlagState.Viewed : required ? FlagState.Required : FlagState.Optional;
        }
    }
}
