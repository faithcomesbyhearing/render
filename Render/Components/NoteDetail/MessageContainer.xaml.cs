﻿using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.NoteDetail
{
    public partial class MessageContainer
    {
        public MessageContainer()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Author, v => v.TextMessageAuthor.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Text, v => v.TextMessageContent.Text));
                d(this.OneWayBind(ViewModel, vm => vm.BarPlayerViewModel, v => v.AudioMessage.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.HasInterpretedAudio, v => v.InterpretedAudioMessage.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.InterpretBarPlayerViewModel, v => v.InterpretedAudioMessage.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.AllowDelete, v => v.MessageTrashButton.IsVisible));
                d(this.BindCommandCustom(MessageTrashButton, v => v.ViewModel.OnDeleteCommand));

                d(this.WhenAnyValue(x => x.ViewModel.IsAuthor)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetMessageStyle));
                
                d(this.WhenAnyValue(x => x.ViewModel.HasAudio)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetMessageType));
            });
        }

        private void SetMessageStyle(bool isOwner)
        {
            Message.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ?
                "OwnerMessage" :
                "Message");

            var hasInterpretedAudio = ViewModel != null && ViewModel.HasInterpretedAudio;
            TextMessage.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ?
                (hasInterpretedAudio ? "OwnerOriginalTextMessage" : "OwnerTextMessage") :
                "TextMessage");

            TextMessageAuthor.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ? 
                (hasInterpretedAudio ? "OwnerTextSlateLightTitle" : "OwnerTextTitle") : 
                "TextTitle");

            TextMessageContent.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ? 
                (hasInterpretedAudio ? "OwnerTextSlateLightContent" : "OwnerTextContent"): 
                "TextContent");

            AudioMessage.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ? 
                (hasInterpretedAudio ? "OwnerOriginalAudioMessage" : "OwnerAudioMessage") :
                (hasInterpretedAudio ? "OriginalAudioMessage" : "AudioMessage"));
            
            if (hasInterpretedAudio)
            {
                InterpretedAudioMessage.Style = ResourceExtensions.GetResourceValue<Style>(Resources, isOwner ? 
                    "OwnerAudioMessage" : 
                    "AudioMessage");
            }

            Message.Opacity = 1;
        }
        
        private void SetMessageType(bool hasAudio)
        {
            AudioMessage.IsVisible = hasAudio;
            TextMessage.IsVisible = !hasAudio;
        }
    }
}