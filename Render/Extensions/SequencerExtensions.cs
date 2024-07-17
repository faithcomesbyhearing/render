using ReactiveUI;
using Render.Components.NoteDetail;
using Render.Interfaces;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Resources;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.ToolbarItems;
using System.Reactive.Linq;

namespace Render.Extensions;

public static class SequencerExtensions
{
    public static ActionViewModelBase CreateActionViewModel(this ISequencerViewModel sequencer,
        IViewModelContextProvider provider,
        List<IDisposable> disposables,
        bool required = false,
        Guid? requirementId = null)
    {
        var viewModel = requirementId.HasValue ?
            new ActionViewModelBase(required ? ActionState.Required : ActionState.Optional, requirementId.Value, string.Empty, provider) :
            new ActionViewModelBase(required ? ActionState.Required : ActionState.Optional, string.Empty, provider);

        disposables.Add(viewModel
            .WhenAnyValue(vm => vm.ActionState)
            .Subscribe(state =>
            {
                var option = state == ActionState.Required ? ItemOption.Required : ItemOption.Optional;

                if (sequencer.GetToolbarItem<IRecordToolbarItem>() is IToolbarItem recordItem)
                {
                    recordItem.Option = option;
                };

                if (sequencer.GetToolbarItem<IPlayToolbarItem>() is IToolbarItem playItem)
                {
                    playItem.Option = option;
                };
            }));

        return viewModel;
    }

    public static void SetupActivityService(this ISequencerViewModel sequencer,
        IViewModelContextProvider provider,
        List<IDisposable> disposables)
    {
        disposables.Add(sequencer
            .WhenAnyValue(vm => vm.State)
            .Subscribe(state =>
            {
                if (state == SequencerState.Playing || state == SequencerState.Recording)
                {
                    provider
                        .GetAudioActivityService()
                        .SetStopCommand(sequencer.StopCommand, true);
                }
            }));
    }

    public static void SetupRecordPermissionPopup(this ISequencerCommonRecorderViewModel sequencer,
        IViewModelContextProvider provider,
        IRenderLogger logger)
    {
        sequencer.HasRecordPermissionCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var modalService = provider.GetModalService();

            try
            {
                var essentials = provider.GetEssentials();
                var hasPermission = await essentials.CheckForAudioPermissions();
                if (hasPermission is false)
                {
                    hasPermission = await essentials.AskForAudioPermissions();

                    logger.LogInfo("Requested audio permissions from user");
                }

                if (hasPermission)
                {
                    return true;
                }

                await modalService.ShowInfoModal(Icon.MicrophoneWarning, AppResources.MicrophoneAccessTitle, AppResources.MicrophoneAccessMessage);
                logger.LogInfo("Microphone is not accessible");
                return false;

            }
            catch (Exception e)
            {
                await modalService.ShowInfoModal(Icon.MicrophoneNotFound, AppResources.MicNotConnectedTitle, AppResources.MicNotConnectedMessage);
                logger.LogError(e);
                return false;
            }
        });
    }

    public static void SetupOnRecordFailedPopup(this ISequencerCommonRecorderViewModel sequencer,
        IViewModelContextProvider provider,
        IRenderLogger logger)
    {
        sequencer.OnRecordFailedCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (sequencer is ISequencerViewModel sequencerViewModel
                        && sequencerViewModel.StopCommand is not null)
                {
                    await sequencerViewModel.StopCommand.Execute();
                }

                var modalService = provider.GetModalService();
                await modalService.ShowInfoModal(Icon.MicrophoneNotFound, AppResources.MicNotConnectedTitle, AppResources.MicNotConnectedMessage);
                logger.LogInfo("Microphone is not connected");
            });
        });

        sequencer.OnRecordDeviceRestoreCommand = ReactiveCommand.Create(() =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var modalService = provider.GetModalService();
                modalService.Close(DialogResult.Ok);
                logger.LogInfo("Microphone is restored");
            });
        });
    }


    public static ConversationViewModel CreateConversationMarker(this ISequencerViewModel sequencer,
        Conversation conversation,
        IViewModelContextProvider provider,
        List<IDisposable> disposables,
        bool isRequired)
    {
        var conversationMarker = new ConversationViewModel(conversation, Components.NotePlacementPlayer.FlagState.Optional, provider, isRequired);

        var disposable = conversationMarker
                    .WhenAnyValue(marker => marker.FlagState)
                    .Subscribe(state =>
                    {
                        var sequencerFlag = sequencer.GetFlag(conversation.Id);
                        if (sequencerFlag is null)
                        {
                            return;
                        }

                        switch (state)
                        {
                            case Components.NotePlacementPlayer.FlagState.Optional:
                                sequencerFlag.State = FlagState.Unread;
                                sequencerFlag.Option = ItemOption.Optional;
                                break;
                            case Components.NotePlacementPlayer.FlagState.Viewed:
                                sequencerFlag.State = FlagState.Read;
                                sequencerFlag.Option = ItemOption.Optional;
                                break;
                            case Components.NotePlacementPlayer.FlagState.Required:
                                sequencerFlag.State = FlagState.Unread;
                                sequencerFlag.Option = ItemOption.Required;
                                break;
                        }
                    });

        disposables.Add(disposable);

        return conversationMarker;
    }

    public static PlayerAudioModel CreatePlayerAudioModel(this Passage passage, string path, FlagType flagType = FlagType.None,
        string endIcon = null, AudioOption option = AudioOption.Optional)
    {
        List<NoteFlagModel> notes = null;

        if (flagType == FlagType.Note)
        {
            notes = CreateNoteFlagModels(passage.CurrentDraftAudio.Conversations);
        }

        return PlayerAudioModel.Create(
            path: path,
            name: string.Format(AppResources.Passage, passage.PassageNumber.PassageNumberString),
            startIcon: Icon.PassageNew.ToString(),
            endIcon: endIcon,
            option: option,
            key: passage.Id,
            flags: notes,
            number: passage.PassageNumber.PassageNumberString);
    }

    public static PlayerAudioModel CreatePlayerAudioModel(this Passage passage,
        IEnumerable<Conversation> conversations,
        string path,
        string name,
        string startIcon,
        string endIcon,
        AudioOption option,
        FlagType flagType = FlagType.None)
    {
        List<NoteFlagModel> notes = null;

        if (flagType == FlagType.Note)
        {
            notes = CreateNoteFlagModels(conversations);
        }

        return PlayerAudioModel.Create(
            path: path,
            name: name,
            startIcon: startIcon,
            endIcon: endIcon,
            key: passage.CurrentDraftAudio.Id,
            option: option,
            flags: notes,
            number: passage.PassageNumber.PassageNumberString);
    }

    public static RecordAudioModel CreateRecordAudioModel(this BackTranslation backTranslation,
        string path,
        FlagType flagType = FlagType.None,
        bool isTemp = false)
    {
        List<NoteFlagModel> notes = null;

        if (flagType == FlagType.Note)
        {
            notes = CreateNoteFlagModels(backTranslation.Conversations);
        }

        return RecordAudioModel.Create(
            path: path,
            name: string.Empty,
            flags: notes,
            isTemp: isTemp);
    }

    private static List<NoteFlagModel> CreateNoteFlagModels(IEnumerable<Conversation> conversations)
    {
        return conversations
            .Select(conversation => new NoteFlagModel(conversation.Id, conversation.FlagOverride, false, false))
            .ToList();
    }

    public static PlayerAudioModel CreatePlayerAudioModel(this Draft draftAudio, ParentAudioType audioType, string name,
        string path, string endIcon = null, AudioOption option = AudioOption.Optional, string number = null, Predicate<Conversation> conversationsFilter = null)
    {
        var conversations = conversationsFilter is null ? draftAudio.Conversations : draftAudio.Conversations.Where(c => conversationsFilter(c));
        var flags = conversations
            .Select(conversation => new NoteFlagModel(conversation.Id, conversation.FlagOverride, false, false))
            .ToList();

        switch (audioType)
        {
            case ParentAudioType.Draft:
            case ParentAudioType.PassageBackTranslation:
            case ParentAudioType.PassageBackTranslation2:
                return PlayerAudioModel.Create(
                    path: path,
                    name: string.Format(AppResources.Passage, name),
                    startIcon: Icon.PassageNew.ToString(),
                    key: draftAudio.Id,
                    flags: flags,
                    endIcon: endIcon,
                    option: option,
                    number: name);
            case ParentAudioType.SegmentBackTranslation:
            case ParentAudioType.SegmentBackTranslation2:
                return PlayerAudioModel.Create(
                    path: path,
                    name: string.Format(AppResources.Segment, name),
                    startIcon: Icon.SegmentNew.ToString(),
                    key: draftAudio.Id,
                    flags: flags,
                    endIcon: endIcon,
                    option: option,
                    number: name);
            default:
                throw new ArgumentOutOfRangeException(nameof(audioType), audioType, null);
        }
    }

    public static RecordAudioModel CreateRecordAudioModel(this Audio audio,
        IEnumerable<Conversation> conversations,
        string path,
        bool isTemp,
        bool canDelete,
        FlagType flagType = FlagType.None)
    {
        List<NoteFlagModel> notes = null;

        if (flagType == FlagType.Note)
        {
            notes = CreateNoteFlagModels(conversations);
        }

        return RecordAudioModel.Create(
            path: path,
            name: string.Empty,
            isTemp: isTemp,
            canDelete: canDelete,
            flags: notes);
    }

    public static EditableAudioModel CreateEditableAudioModel(this Audio audio, string path, Guid key)
    {
        return EditableAudioModel.Create(
            path: path,
            key: key);
    }
}
