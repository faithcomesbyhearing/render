using DynamicData;
using ReactiveUI;
using Render.Components.NoteDetail;
using Render.Extensions;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages;
using Render.Repositories.Extensions;
using Render.Sequencer.Contracts.Interfaces;

namespace Render.Kernel
{
    public class ConversationService : IDisposable
    {
        private Dictionary<Guid, SequencerNoteDetailViewModel> _sequencerNoteDetailViewModels = new();
        private readonly WorkflowPageBaseViewModel _page;
        private readonly List<IDisposable> _pageDisposables;
        private readonly Step _step;
        private readonly Stage _stage;
        public ParentAudioType ParentAudioType;
        private readonly ISequencerViewModel _sequencerViewModel;
        private readonly bool _appendNotesForChildAudios;

        public List<Guid> AllowedStageIdsForDrawingFlags = new();
        public IEnumerable<Draft> SequencerAudios;
        public Dictionary<Guid, NotableAudio> SequencerRecords;
        public Action TapFlagPostEvent { get; set; }

        public ConversationService(
            WorkflowPageBaseViewModel page,
            List<IDisposable> pageDisposables,
            Stage stage,
            Step step,
            ISequencerViewModel sequencerViewModel,
            ParentAudioType parentAudioType = ParentAudioType.Draft,
            bool appendNotesForChildAudios = false)
        {
            _page = page;
            _pageDisposables = pageDisposables;
            _stage = stage;
            _step = step;
            _sequencerViewModel = sequencerViewModel;
            ParentAudioType = parentAudioType;
            _appendNotesForChildAudios = appendNotesForChildAudios;
           
            _sequencerViewModel.AddFlagCommand =
                ReactiveCommand.CreateFromTask<IFlag, bool>(ShowConversationAsync);
            _sequencerViewModel.TapFlagCommand = ReactiveCommand.CreateFromTask(
                async (IFlag flag) =>
                {
                    await ShowConversationAsync(flag);
                    TapFlagPostEvent?.Invoke();
                });
        }

        public static IEnumerable<Conversation> GetAdditionalNotes(Draft audio)
        {
            var passageRetells = audio.RetellBackTranslationAudio?.Conversations ?? new List<Conversation>();
            passageRetells.ForEach(c =>
            {
                c.ParentAudioType = ParentAudioType.PassageBackTranslation;
                c.FlagOverride = 0;
                c.ParentAudio = audio.RetellBackTranslationAudio;
            });

            var segmentRetellsWithAudio = audio.SegmentBackTranslationAudios
                .Select(sbt => new { SegmentRetell = sbt, sbt.Conversations }).ToList();
            segmentRetellsWithAudio.ForEach(sr => sr.Conversations.ForEach(c =>
            {
                c.ParentAudioType = ParentAudioType.SegmentBackTranslation;
                c.FlagOverride = 0;
                c.ParentAudio = sr.SegmentRetell;
            }));
            var segmentRetells = segmentRetellsWithAudio.SelectMany(sr => sr.Conversations);

            var passageRetells2 = audio.RetellBackTranslationAudio?.RetellBackTranslationAudio?.Conversations ??
                                  new List<Conversation>();
            passageRetells2.ForEach(c =>
            {
                c.ParentAudioType = ParentAudioType.PassageBackTranslation2;
                c.FlagOverride = 0;
                c.ParentAudio = audio.RetellBackTranslationAudio.RetellBackTranslationAudio;
            });

            var segmentRetellsWithAudio2 = audio.SegmentBackTranslationAudios
                .Select(sbt => sbt.RetellBackTranslationAudio)
                .Where(r => r != null)
                .Select(rbt => new { SegmentRetell = rbt, rbt.Conversations }).ToList();
            segmentRetellsWithAudio2.ForEach(sr => sr.Conversations.ForEach(c =>
            {
                c.ParentAudioType = ParentAudioType.SegmentBackTranslation2;
                c.FlagOverride = 0;
                c.ParentAudio = sr.SegmentRetell;
            }));
            var segmentRetells2 = segmentRetellsWithAudio2.SelectMany(sr => sr.Conversations);

            var conversations = passageRetells.Union(segmentRetells).Union(passageRetells2).Union(segmentRetells2);

            return conversations;
        }

        public IEnumerable<Conversation> GetConversations(Draft audio)
        {
            audio.Conversations.ForEach(c => c.ParentAudioType = ParentAudioType);

            var conversations = _appendNotesForChildAudios
                ? audio.Conversations.Union(GetAdditionalNotes(audio))
                : audio.Conversations;

            return conversations.Where(c => AllowedStageIdsForDrawingFlags.Contains(c.StageId));
        }

        public IEnumerable<Conversation> GetConversations(Audio audio)
        {
            if (audio is NotableAudio)
            {
                var notableAudio = (NotableAudio) audio;
                notableAudio.Conversations.ForEach(c => c.ParentAudioType = ParentAudioType);

                return notableAudio.Conversations.Where(c => AllowedStageIdsForDrawingFlags.Contains(c.StageId));
            }

            return Array.Empty<Conversation>();
        }

        public IEnumerable<Conversation> GetConversations(IEnumerable<Draft> audios)
        {
            return audios.SelectMany(a => a.Conversations)
                .Where(c => AllowedStageIdsForDrawingFlags.Contains(c.StageId));
        }

        public void InitializeNoteDetail(bool isRequired, bool allowEditing = true)
        {
            _sequencerNoteDetailViewModels.ForEach(x =>
            {
                if (x.Value == null) return;

                _page.ActionViewModelBaseSourceList.RemoveMany(x.Value.ConversationMarkers.SourceItems);
                x.Value?.Dispose();
            });

            _sequencerNoteDetailViewModels.Clear();

            foreach (var audio in SequencerAudios)
            {
                var sequencerNoteDetailViewModel = new SequencerNoteDetailViewModel(
                    GetConversations(audio),
                    _page.Section,
                    _stage,
                    _sequencerViewModel,
                    _page.ViewModelContextProvider,
                    _page.ActionViewModelBaseSourceList,
                    _pageDisposables,
                    conversationMarkersRequired: isRequired,
                    allowEditing: allowEditing,
                    conversationsFilter: c => AllowedStageIdsForDrawingFlags.Contains(c.StageId));

                sequencerNoteDetailViewModel.SaveCommand =
                    ReactiveCommand.CreateFromTask<Conversation>(SaveDraftWithNoteAsync);
                sequencerNoteDetailViewModel.DeleteMessageCommand =
                    ReactiveCommand.CreateFromTask<Message>(DeleteMessageAsync);

                sequencerNoteDetailViewModel.AddMessageCommand = ReactiveCommand.Create((Message _) =>
                {
                    _page.ViewModelContextProvider.GetWorkflowService()
                        .SetHasNewMessageForWorkflowStep(_page.Section, _step, true);
                });

                if (_sequencerNoteDetailViewModels.All(vm => vm.Key != audio.Id))
                {
                    _sequencerNoteDetailViewModels.Add(audio.Id, sequencerNoteDetailViewModel);
                }
            }
        }

        public void InitializeNoteDetailForRecord(bool isRequired, bool allowEditing = true)
        {
            _sequencerNoteDetailViewModels.ForEach(x =>
            {
                if (x.Value == null) return;

                _page.ActionViewModelBaseSourceList.RemoveMany(x.Value.ConversationMarkers.SourceItems);
                x.Value?.Dispose();
            });

            _sequencerNoteDetailViewModels.Clear();

            foreach (var audio in SequencerRecords)
            {
                var sequencerNoteDetailViewModel = new SequencerNoteDetailViewModel(
                    GetConversations(audio.Value),
                    _page.Section,
                    _stage,
                    _sequencerViewModel,
                    _page.ViewModelContextProvider,
                    _page.ActionViewModelBaseSourceList,
                    _pageDisposables,
                    conversationMarkersRequired: isRequired,
                    allowEditing: allowEditing,
                    conversationsFilter: c => AllowedStageIdsForDrawingFlags.Contains(c.StageId));

                sequencerNoteDetailViewModel.SaveCommand =
                    ReactiveCommand.CreateFromTask<Conversation>(SaveDraftWithNoteAsync);
                sequencerNoteDetailViewModel.DeleteMessageCommand =
                    ReactiveCommand.CreateFromTask<Message>(DeleteMessageAsync);

                sequencerNoteDetailViewModel.AddMessageCommand = ReactiveCommand.Create((Message _) =>
                {
                    _page.ViewModelContextProvider.GetWorkflowService()
                        .SetHasNewMessageForWorkflowStep(_page.Section, _step, true);
                });

                if (_sequencerNoteDetailViewModels.All(vm => vm.Key != audio.Value.Id))
                {
                    _sequencerNoteDetailViewModels.Add(audio.Key, sequencerNoteDetailViewModel);
                }
            }
        }

        public void DefineFlagsToDraw(List<Snapshot> snapshots, Guid currentStageId)
        {
            var previousStage = snapshots.LastOrDefault(x => x.StageId != currentStageId);
            switch (previousStage?.StageName)
            {
                case "Draft":
                    AllowedStageIdsForDrawingFlags.AddRange(new List<Guid>()
                        { previousStage.StageId, currentStageId });
                    break;
                default:
                    AllowedStageIdsForDrawingFlags.Add(currentStageId);
                    break;
            }
        }

        private Guid GetCurrentAudioId()
        {
            var playerKey = (_sequencerViewModel as ISequencerPlayerViewModel)?.GetCurrentAudio()?.Key ?? Guid.Empty;
            if (playerKey == Guid.Empty)
            {
                var preservedAudioKey = SequencerAudios.FirstOrDefault().Id;
                var matchKey = SequencerRecords.FirstOrDefault(a => a.Key == preservedAudioKey).Key;
                return matchKey;
            }
            return playerKey;
        }

        private async Task<bool> ShowConversationAsync(IFlag flag)
        {
            if (flag.Key == default)
            {
                var audioId = GetCurrentAudioId();

                return await _sequencerNoteDetailViewModels[audioId].ShowConversationAsync(flag);
            }

            var sequencers = _sequencerNoteDetailViewModels.Where(s => s.Value.Conversations.Any(c => c.Id == flag.Key));

            if (!sequencers.Any())
            {
                return false;
            }

            return await sequencers.First().Value.ShowConversationAsync(flag);
        }

        private async Task SaveDraftWithNoteAsync(Conversation conversation)
        {
            Draft audio;

            if (conversation.ParentAudio == null)
            {
                var audioId = GetCurrentAudioId();

                audio = SequencerAudios.FirstOrDefault(a => a.Id == audioId);
            }
            else
            {
                audio = conversation.ParentAudio;
            }

            if (audio != null)
            {
                audio.UpdateOrDeleteConversation(conversation);

                if (audio is SegmentBackTranslation segment)
                {
                    var segmentRepository = _page.ViewModelContextProvider.GetSegmentBackTranslationRepository();
                    await segmentRepository.SaveAsync(segment);
                }
                else if (audio is RetellBackTranslation retell)
                {
                    var retellRepository = _page.ViewModelContextProvider.GetRetellBackTranslationRepository();
                    await retellRepository.SaveAsync(retell);
                }
                else
                {
                    var draftRepository = _page.ViewModelContextProvider.GetDraftRepository();
                    await draftRepository.SaveAsync(audio);
                }

                TapFlagPostEvent?.Invoke();
            }
        }

        private async Task DeleteMessageAsync(Message message)
        {
            var conversations = SequencerAudios.SelectMany(GetConversations).ToList();
            foreach (var conversation in conversations)
            {
                var removed = conversation.Messages.Remove(message);
                if (removed)
                {
                    await SaveDraftWithNoteAsync(conversation);
                    break;
                }
            }

            if (conversations.IsNullOrEmpty())
            {
				await _page.ViewModelContextProvider.GetWorkflowService()
						.SetHasNewMessageForWorkflowStep(_page.Section, _step, false);
			}
        }

        public void Dispose()
        {
            _sequencerNoteDetailViewModels.Values.DisposeCollection();
            _sequencerNoteDetailViewModels = null;

            SequencerAudios = null;

            SequencerRecords?.Clear();
            SequencerRecords = null;

            AllowedStageIdsForDrawingFlags.Clear();
            AllowedStageIdsForDrawingFlags = null;
        }
    }
}