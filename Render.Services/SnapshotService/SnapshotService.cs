using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Services.StageService;
using Render.Services.WorkflowService;
using Render.TempFromVessel;

namespace Render.Services.SnapshotService
{
    public class SnapshotService : ISnapshotService
    {
        private class SnapshotResetParams
        {
            internal Snapshot LastTempSnapshot { get; set; }
            internal Section Section { get; set; }
            internal Stage PreviousStage { get; set; }
            internal Step PreviousStep { get; set; }
            internal Guid UserId { get; set; }
        }

        private readonly ISnapshotRepository _snapshotRepository;
        private readonly IWorkflowService _workflowService;
        private readonly IStageService _stageService;
        private readonly ISectionRepository _sectionRepository;
        private readonly IAudioRepository<Audio> _audioRepository;
        private readonly IAudioRepository<Draft> _draftRepository;
        private readonly IAudioRepository<RetellBackTranslation> _retellRepository;
        private readonly IAudioRepository<SegmentBackTranslation> _segmentRepository;
        private readonly IUserRepository _userRepository;

        public SnapshotService(
            ISnapshotRepository snapshotRepository,
            IWorkflowService workflowService,
            IStageService stageService,
            ISectionRepository sectionRepository,
            IAudioRepository<Audio> audioRepository,
            IAudioRepository<Draft> draftRepository,
            IAudioRepository<RetellBackTranslation> retellRepository,
            IAudioRepository<SegmentBackTranslation> segmentRepository,
            IUserRepository userRepository)
        {
            _snapshotRepository = snapshotRepository;
            _workflowService = workflowService;
            _stageService = stageService;
            _sectionRepository = sectionRepository;
            _audioRepository = audioRepository;
            _draftRepository = draftRepository;
            _retellRepository = retellRepository;
            _segmentRepository = segmentRepository;
            _userRepository = userRepository;
        }

        public async Task ReturnBack(
            List<Stage> workflowStages,
            Section section,
            Stage currentStage,
            Stage previousStage,
            Step previousStep,
            Guid userId)
        {
            var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(section.Id);
            var lastTempSnapshot = snapshots.FirstOrDefault(s => s.StageId == currentStage.Id);
            var previousStageSnapshots = snapshots.Where(snapshot => snapshot.StageId == previousStage.Id);
            var snapshotsFromRemovedStages = FilterSnapshotsWithoutStages(snapshots, workflowStages);

            var snapshotReturnInfo = new SnapshotResetParams
            {
                LastTempSnapshot = lastTempSnapshot,
                Section = section,
                PreviousStage = previousStage,
                PreviousStep = previousStep,
                UserId = userId
            };

            var previousStageContainsSnapshot = snapshots.Any(snapshot => snapshot.StageId == previousStage.Id);

            if (!previousStageContainsSnapshot)
            {
                await MoveNext(snapshotsFromRemovedStages, snapshotReturnInfo);
            }
            else
            {
                await MoveBack(snapshotsFromRemovedStages, previousStageSnapshots, snapshotReturnInfo);
            }
        }

        public async Task CreateTemporarySnapshotAfterReRecording(Section section, Step step, Guid userId,
            Guid parentSnapshotId)
        {
            //Look for new drafts
            var snapshot = (await _snapshotRepository.GetSnapshotsForSectionAsync(section.Id)).Last();
            snapshot = await _snapshotRepository.GetPassageDraftsForSnapshot(snapshot);
            var latestDraftIds = snapshot.PassageDrafts.Select(x => x.DraftId);
            var currentDraftIds = section.Passages.Select(x => x.CurrentDraftAudio.Id);

            if (!latestDraftIds.SequenceEqual(currentDraftIds))
            {
                //Create snapshot within the revise loop and set its delete flag to true to be cleaned up once we advance the
                //section to the another stage
                var workflow = _workflowService.FindWorkflow(step.Id);
                var stage = workflow.GetStage(step.Id);
                await CreateSnapshotAsync(section, stage.Id, step.Id, stage.Name, userId, parentSnapshotId, temporary: true);
                await _workflowService.SetHasNewDraftsForWorkflowStep(section, step, true);
            }
        }

        /// <summary>
        /// Create snapshot for stages with missing snapshot when previous stage was removed (RemoveWork).
        /// </summary>
        /// <returns></returns>
        public async Task CreateMissingSnapshotsForStagesWithPreviousRemovedStage(Guid userId)
        {
            if (_workflowService.WorkflowStatusListByWorkflow?.Count < 1)
            {
                return;
            }

            var renderWorkflows = _workflowService?.WorkflowStatusListByWorkflow?.Keys.Cast<RenderWorkflow>().ToList();

            for (int i = 0; i < renderWorkflows?.Count; i++)
            {
                var workflow = renderWorkflows[i];
                var allStages = workflow.GetCustomStages(true).ToArray();
                var stageSteps = _stageService.FindWorkflowStepsAssignedForUser(userId, workflow).ToList();

                if (allStages.Count() < 2 || stageSteps.Count < 1)
                {
                    continue;
                }

                foreach (var stepPair in _stageService.StepsWithWork)
                {
                    var stepId = stepPair.Key;

                    foreach (var sectionId in stepPair.Value)
                    {
                        if (stageSteps.All(s => s.StepId != stepId))
                        {
                            continue;
                        }

                        var stageId = stageSteps.First(s => s.StepId == stepId).StageId;

                        if (allStages.All(s => s.Id != stageId))
                        {
                            continue;
                        }

                        var stage = allStages.First(s => s.Id == stageId);
                        var stageIndex = allStages.IndexOf(stage);

                        var previousStage = stageIndex >= 1 ? allStages[stageIndex - 1] : null;

                        if (previousStage == null || previousStage.State != StageState.RemoveWork)
                        {
                            continue;
                        }

                        var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(sectionId);
                        var filteredSnapshots = _snapshotRepository.FilterSnapshotByStageId(snapshots, stageId);

                        if (filteredSnapshots.Count > 0)
                        {
                            continue;
                        }

                        var section = await _sectionRepository.GetSectionWithReferencesAsync(sectionId);
                        // create temp snapshot for the current step, we do not need to create snapshot for the next stage (RemoveOldTemporarySnapshot)
                        // becouse this will be done during current stage finalization and this will cause snapshot conflict
                        await CreateSnapshotAsync(section, stageId, stepId, stage.Name, userId, default, temporary: true);
                    }
                }
            }
        }

        public async Task RemoveOldTemporarySnapshot(
            RenderWorkflow workflow,
            Section section,
            Guid stepId,
            List<Step> nextSteps,
            Guid userId,
            bool needToCreateSnapshotForCurrentStep = true,
            bool needToCreateSnapshotForNextStage = true)
        {
            var currentStage = workflow.GetStage(stepId);
            var nextStage = workflow.GetStage(nextSteps.Any() ? nextSteps.First().Id : Guid.Empty);
            if (nextStage != null && nextStage.Id != currentStage.Id)
            {
                if (needToCreateSnapshotForCurrentStep)
                {
                    var previousSnapshot = (await _snapshotRepository.GetPermanentSnapshotsForSectionAsync(section.Id))
                        .LastOrDefault(x => x.StageId != currentStage.Id);
                    var parentSnapshotId = previousSnapshot?.Id ?? Guid.Empty;
                    var recentSnapshot = await CreateSnapshotAsync(section, currentStage.Id, stepId, currentStage.Name, userId, parentSnapshotId, temporary: false);

                    if (currentStage.StageType is StageTypes.ConsultantCheck)
                    {
                        await UpdateRetellAudiosWithSnapshotInfo(section, recentSnapshot);
                    }
                }

                var snapshotsToDelete = (await _snapshotRepository.GetSnapshotsForSectionAsync(section.Id))
                    .Where(x => x.Temporary).ToList();
                if (snapshotsToDelete.Any())
                {
                    await _snapshotRepository.BatchSoftDeleteAsync(snapshotsToDelete, section);
                }

                // create temporary snapshot upon entry of next stage
                if (needToCreateSnapshotForNextStage)
                {
                    await CreateSnapshotAsync(section, nextStage.Id, stepId, nextStage.Name, userId, default, temporary: true);
                }
            }
        }

        /// <summary>
        /// This method gets all sections at a particular step that do not have snapshot conflicts. This allows us
        /// to skip sections with conflicts both in displaying the icon color, and navigation.
        /// </summary>
        public async Task<List<Guid>> FilterOutConflicts(Step step)
        {
            var allSectionsAtStep = _stageService.SectionsAtStep(step.Id);
            //Check for snapshot conflicts
            var sectionsAtStep = new List<Guid>();
            foreach (var sectionId in allSectionsAtStep)
            {
                if (!await CheckForSnapshotConflicts(sectionId))
                {
                    sectionsAtStep.Add(sectionId);
                }
            }

            return sectionsAtStep;
        }

        public async Task<bool> CheckForSnapshotConflicts(Guid sectionId)
        {
            var conflictedStageIds = await GetConflictedSnapshots(sectionId);
            return conflictedStageIds.Any();
        }

        public async Task<Stage> GetConflictedStage(Guid sectionId)
        {
            var conflictedStageIds = await GetConflictedSnapshots(sectionId);
            return _workflowService.ProjectWorkflow.GetAllStages().FirstOrDefault(x => x.Id == conflictedStageIds.FirstOrDefault());
        }

        public async Task<List<ConflictedSnapshot>> GetLastConflictedSnapshots(Guid sectionId)
        {
            var snapshots = await _snapshotRepository.GetPermanentSnapshotsForSectionAsync(sectionId);

            if (snapshots.Any() is false)
            {
                return new List<ConflictedSnapshot>();
            }

            var lastWorkedSnapshotId = snapshots
                .OrderByDescending(snapshot => snapshot.DateUpdated)
                .Select(snapshot => snapshot.Id)
                .FirstOrDefault();

            var firstGroup = snapshots.FindRelatedSnapshots(lastWorkedSnapshotId);
            var translator1 = await GetTranslatorName(firstGroup);

            var remainingSnapshots = snapshots.Except(firstGroup);
            var secondLastWorkedSnapshotId = remainingSnapshots
                    .OrderByDescending(snapshot => snapshot.DateUpdated)
                    .Select(snapshot => snapshot.Id)
                    .FirstOrDefault();

            var secondGroup = snapshots.FindRelatedSnapshots(secondLastWorkedSnapshotId);
            var translator2 = await GetTranslatorName(secondGroup);

            var snapshot1 = firstGroup.First();
            var snapshot2 = secondGroup.First();

            await FillSnapshotWithAudio(snapshot1);
            await FillSnapshotWithAudio(snapshot2);

            return new List<ConflictedSnapshot>
            {
                { new ConflictedSnapshot { TranslatorName = translator1, Snapshot = snapshot1 } },
                { new ConflictedSnapshot { TranslatorName = translator2, Snapshot = snapshot2 } },
            };
        }

        private async Task<List<Guid>> GetConflictedSnapshots(Guid sectionId)
        {
            var snapshots = await _snapshotRepository.GetPermanentSnapshotsForSectionAsync(sectionId);
            return snapshots.GroupBy(x => x.StageId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
        }

        private async Task<Snapshot> CreateSnapshotAsync(
            Section section,
            Guid stageId,
            Guid stepId,
            string stageName,
            Guid userId,
            Guid parentSnapshotId,
            bool temporary)
        {
            var sectionReferenceAudioSnapshots = new List<SectionReferenceAudioSnapshot>();
            if (section.References != null)
            {
                sectionReferenceAudioSnapshots = section.References.Select(
                    reference => new SectionReferenceAudioSnapshot(reference.Id, reference.LockedReferenceByPassageNumbersList,
                        reference.PassageReferences.ToList())).ToList();
            }

            var noteInterpretationIds = section.GetAllNoteInterpretationIdsForSection();
            var snapshot = new Snapshot(
                sectionId: section.Id,
                checkedBy: section.CheckedBy,
                approvedBy: section.ApprovedBy,
                approvedDate: section.ApprovedDate,
                createdBy: userId,
                scopeId: section.ScopeId,
                projectId: section.ProjectId,
                stageId: stageId,
                stepId: stepId,
                passages: section.Passages.ToList(),
                sectionReferenceAudioSnapshots: sectionReferenceAudioSnapshots,
                noteInterpretationIds: noteInterpretationIds,
                stageName: stageName,
                parentSnapshot: parentSnapshotId,
                temporary: temporary);

            await _snapshotRepository.SaveAsync(snapshot);

            return snapshot;
        }

        private async Task MoveNext(IEnumerable<Snapshot> snapshotsFromRemovedStages, SnapshotResetParams snapshotResetParams)
        {
            await RemoveSnapshots(snapshotsFromRemovedStages, snapshotResetParams.Section);
            await ResetToStage(snapshotResetParams);
        }

        private async Task MoveBack(
            IEnumerable<Snapshot> snapshotsFromRemovedStages,
            IEnumerable<Snapshot> previousStageSnapshots,
            SnapshotResetParams snapshotResetParams)
        {
            await RemoveSnapshots(snapshotsFromRemovedStages, snapshotResetParams.Section);
            await RemoveSnapshots(previousStageSnapshots, snapshotResetParams.Section);
            await ResetToStage(snapshotResetParams);
        }

        private async Task RemoveSnapshots(IEnumerable<Snapshot> snapshots, Section section)
        {
            await RemoveIntermediateSnapshotsWithAudio(snapshots, section);
            await RemovePermanentSnapshot(snapshots);
        }

        private async Task RemoveIntermediateSnapshotsWithAudio(IEnumerable<Snapshot> snapshotsToSoftDelete, Section section)
        {
            var temporarySnapshots = snapshotsToSoftDelete.Where(s => s.Temporary).ToList();
            await _snapshotRepository.BatchSoftDeleteAsync(temporarySnapshots, section);
        }

        private async Task RemovePermanentSnapshot(IEnumerable<Snapshot> snapshotsToSoftDelete)
        {
            var permanentSnapshots = snapshotsToSoftDelete.Where(s => !s.Temporary);
            await SetDeleted(permanentSnapshots);
        }

        private async Task ResetToStage(SnapshotResetParams snapshotResetParams)
        {
            await SetDeleted(snapshotResetParams.LastTempSnapshot);
            await CreateSnapshotAsync(
                snapshotResetParams.Section,
                snapshotResetParams.PreviousStage.Id,
                snapshotResetParams.PreviousStep.Id,
                snapshotResetParams.PreviousStage.Name,
                snapshotResetParams.UserId,
                parentSnapshotId: default,
                temporary: true);
        }

        private async Task SetDeleted(Snapshot snapshot)
        {
            snapshot.SetDeleted(true);
            await _snapshotRepository.SaveAsync(snapshot);
        }

        private async Task SetDeleted(IEnumerable<Snapshot> snapshots)
        {
            foreach (var snapshotToDelete in snapshots)
            {
                if (snapshotToDelete is null) continue;

                snapshotToDelete.SetDeleted(true);
                await _snapshotRepository.SaveAsync(snapshotToDelete);
            }
        }

        private IEnumerable<Snapshot> FilterSnapshotsWithoutStages(IEnumerable<Snapshot> allSnapshots, IEnumerable<Stage> allStages)
        {
            return allSnapshots
                .Where(snapshot => allStages
                    .Select(stage => stage.Id)
                    .Contains(snapshot.StageId) is false);
        }

        private async Task UpdateRetellAudiosWithSnapshotInfo(Section section, Snapshot recentSnapshot)
        {
            foreach (var passage in section.Passages)
            {
                await _sectionRepository.UpdateRetellsWithSnapshotInfo(passage, recentSnapshot);
            }
        }

        private async Task FillSnapshotWithAudio(Snapshot snapshot)
        {
            var passagesAreEmpty = snapshot.Passages.Any(p => p.HasAudio is false);
            if (passagesAreEmpty)
            {
                await FillSnapshotPassagesWithParentAudio(snapshot);
            }

            await FillSnapshotWithRetellsAndNotes(snapshot);
        }

        private async Task FillSnapshotPassagesWithParentAudio(Snapshot snapshot)
        {
            foreach (var passage in snapshot.Passages)
            {
                Draft draft;
                // for backward compatibility support
                if (passage.CurrentDraftAudioId != Guid.Empty)
                {
                    draft = await _draftRepository.GetByIdAsync(passage.CurrentDraftAudioId);
                }
                else
                {
                    // get a draft for a snapshot using the unique DraftId key
                    var draftId = snapshot.PassageDrafts.Single(passageDraft => passageDraft.PassageId == passage.Id).DraftId;
                    draft = await _draftRepository.GetByIdAsync(draftId);
                }
                if (draft is null) continue;
                passage.ChangeCurrentDraftAudio(draft);
            }
        }

        private async Task FillSnapshotWithRetellsAndNotes(Snapshot snapshot)
        {
            foreach (var passage in snapshot.Passages)
            {
                foreach (var message in passage.CurrentDraftAudio.Conversations.SelectMany(conversation => conversation.Messages))
                {
                    if (message.Media.AudioId != Guid.Empty)
                    {
                        message.Media.Audio = await _audioRepository.GetByIdAsync(message.Media.AudioId);
                    }
                }

                await FillPassageWithRetellAudio(snapshot, passage);
                await FillPassageWithSegmentAudios(snapshot, passage);
            }
        }

        private async Task FillPassageWithRetellAudio(Snapshot correspondedSnapshot, Passage passage)
        {
            if (passage.CurrentDraftAudio is null)
            {
                return;
            }

            var retells = await _retellRepository.GetMultipleByParentIdAsync(passage.CurrentDraftAudio.Id);
            var retellAudio = retells.FirstOrDefault(retell => retell.CorrespondedSnashotId == correspondedSnapshot.Id);

            //for back compatibility: FROM PBI: 33693 and PBI: 31422
            if (retellAudio is null)
            {
                await _sectionRepository.GetPassageDraftsAsync(correspondedSnapshot, getRetellBackTranslations: true);
                return;
            }
            await FillPassageWithSecondRetellAudio(retellAudio);

            passage.CurrentDraftAudio.RetellBackTranslationAudio = retellAudio;
        }

        private async Task FillPassageWithSegmentAudios(Snapshot correspondedSnapshot, Passage passage)
        {
            if (passage.CurrentDraftAudio is null)
            {
                return;
            }

            var segments = await _segmentRepository.GetMultipleByParentIdAsync(passage.CurrentDraftAudio.Id);
            if (segments is null)
            {
                return;
            }

            var segmentAudios = segments
                .OrderBy(x => x.TimeMarkers.StartMarkerTime)
                .GroupBy(segment => segment.CorrespondedSnashotId)
                .Where(group => group.Key == correspondedSnapshot.Id)
                .SelectMany(group => group)
                .ToList();

            //for back compatibility: FROM PBI: 33693 and PBI: 31422
            if (segmentAudios.Count() == 0)
            {
                await _sectionRepository.GetPassageDraftsAsync(correspondedSnapshot, getSegmentBackTranslations: true);
                return;
            }

            await FillPassageWithSecondSegmentAudios(segmentAudios);

            passage.CurrentDraftAudio.SegmentBackTranslationAudios = segmentAudios;
        }

        private async Task FillPassageWithSecondRetellAudio(RetellBackTranslation firstStepRetell)
        {
            if (firstStepRetell is null)
            {
                return;
            }

            var secondRetell = await _retellRepository.GetByParentIdAsync(firstStepRetell.Id);
            firstStepRetell.RetellBackTranslationAudio = secondRetell;
        }

        private async Task FillPassageWithSecondSegmentAudios(List<SegmentBackTranslation> firstStepSegments)
        {
            if (firstStepSegments.Count() == 0)
            {
                return;
            }

            foreach (var segment in firstStepSegments)
            {
                if (segment is null)
                {
                    continue;
                }

                segment.RetellBackTranslationAudio = await _retellRepository.GetByParentIdAsync(segment.Id);
            }
        }

        private async Task<string> GetTranslatorName(List<Snapshot> snapshotChain)
        {
            var draftSnapshot = snapshotChain.LastOrDefault(snapshot => snapshot.StageName.Contains(Stage.DraftingDefaultStageName));
            if (draftSnapshot is not null)
            {
                var user = await _userRepository.GetUserAsync(draftSnapshot.CreatedBy);
                if (user is not null)
                {
                    return string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
                }
            }

            return string.Empty;
        }

        public void Dispose()
        {
            _snapshotRepository?.Dispose();
            _sectionRepository?.Dispose();
            _audioRepository?.Dispose();
            _draftRepository?.Dispose();
            _retellRepository?.Dispose();
            _segmentRepository?.Dispose();
            _userRepository?.Dispose();
        }
    }
}