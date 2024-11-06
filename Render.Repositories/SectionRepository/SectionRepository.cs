using Render.Interfaces;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Snapshot;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;

namespace Render.Repositories.SectionRepository
{
    public class SectionRepository : ISectionRepository
    {
        private readonly IDataPersistence<Section> _sectionPersistence;
        private readonly IAudioRepository<SectionReferenceAudio> _sectionReferenceAudioRepository;
        private readonly IAudioRepository<Draft> _draftRepository;
        private readonly IAudioRepository<Models.Audio.Audio> _audioRepository;
        private readonly IAudioRepository<RetellBackTranslation> _retellRepository;
        private readonly IAudioRepository<SegmentBackTranslation> _segmentRepository;
        private readonly IAudioRepository<NotableAudio> _notableAudioRepository;
        private readonly IAudioRepository<Response> _responseRepository;
        private readonly IAudioRepository<CommunityRetell> _communityRetellAudioRepository;
        private readonly IDataPersistence<Reference> _referencePersistence;
        private readonly ICommunityTestRepository _communityTestRepository;
        private readonly IRenderLogger _logger;

        public SectionRepository(
            IRenderLogger logger,
            IDataPersistence<Section> sectionPersistence,
            IAudioRepository<SectionReferenceAudio> sectionReferenceAudioRepository,
            IAudioRepository<Draft> draftRepository,
            IAudioRepository<Models.Audio.Audio> audioRepository,
            IAudioRepository<RetellBackTranslation> retellRepository,
            IAudioRepository<SegmentBackTranslation> segmentRepository,
            IAudioRepository<NotableAudio> notableAudioRepository,
            IAudioRepository<Response> responseRepository,
            IAudioRepository<CommunityRetell> communityRetellAudioRepository,
            IDataPersistence<Reference> referencePersistence,
            ICommunityTestRepository communityTestRepository
        )
        {
            _logger = logger;
            _sectionPersistence = sectionPersistence;
            _sectionReferenceAudioRepository = sectionReferenceAudioRepository;
            _draftRepository = draftRepository;
            _audioRepository = audioRepository;
            _retellRepository = retellRepository;
            _segmentRepository = segmentRepository;
            _referencePersistence = referencePersistence;
            _communityTestRepository = communityTestRepository;
            _notableAudioRepository = notableAudioRepository;
            _responseRepository = responseRepository;
            _communityRetellAudioRepository = communityRetellAudioRepository;
        }

        public async Task<Section> GetSectionAsync(Guid id, bool withReferences = false)
        {
            var section = await _sectionPersistence.GetAsync(id);
            if (section == null || section.Deleted) return null;
            section = await GetSectionTitleAudioAsync(section);
            if (withReferences)
            {
                return await GetReferencesForSectionAsync(section);
            }

            return section;
        }

        public async Task<Section> GetSectionWithDraftsAsync(
            Guid id,
            bool getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false,
            bool getCommunityTest = false,
            bool withReferences = false)
        {
            var section = await _sectionPersistence.GetAsync(id);
            if (section == null || section.Deleted) return null;
            if (withReferences)
            {
                await GetReferencesForSectionAsync(section);
            }

            section = await GetSectionTitleAudioAsync(section);
            return await GetPassageDraftsAsync(
                section: section,
                getRetellBackTranslations: getRetellBackTranslations,
                getSegmentBackTranslations: getSegmentBackTranslations,
                getCommunityTest: getCommunityTest);
        }

        public async Task<Section> GetSectionWithReferencesAsync(Guid id)
        {
            var section = await _sectionPersistence.GetAsync(id);
            if (section == null || section.Deleted) return null;

            section = await GetSectionTitleAudioAsync(section);

            var sectionReferenceAudioList = await _sectionReferenceAudioRepository.GetMultipleByParentIdAsync(section.Id);
            foreach (var sectionReferenceAudio in sectionReferenceAudioList)
            {
                sectionReferenceAudio.Reference = await _referencePersistence.GetAsync(sectionReferenceAudio.ReferenceId);
            }

            section.References = section.References = sectionReferenceAudioList;

            var sectionMaterials = await _audioRepository.GetMultipleByParentIdAsync(section.Id);
            foreach (var supplementaryMaterial in section.SupplementaryMaterials)
            {
                supplementaryMaterial.Audio = sectionMaterials.SingleOrDefault(x => x.Id == supplementaryMaterial.AudioId);
            }

            return await GetPassageDraftsAsync(section);
        }

        public async Task<List<Section>> GetSectionsForProjectAsync(Guid projectId)
        {
            var sections = (await _sectionPersistence.QueryOnFieldAsync(nameof(Section.ProjectId), projectId.ToString(), 0))
                .Where(x => !x.Deleted)
                .ToList();

            // when the scope of a section is updated in LaunchPad (i.e., audio and/or markers have been updated)
            // a new section with new Id is created and the deployment version number is incremented
            // the outdated section still exists
            // we need to filtering out the outdated sections by marking them as "deleted"
            var duplicateSectionsByNumber = sections
                .GroupBy(x => new { x.ScopeId, x.Number })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicateSections in duplicateSectionsByNumber)
            {
                var latestDeploymentNumber = duplicateSections.Max(x => x.DeploymentVersionNumber);
                foreach (var conflictsSection in duplicateSections)
                {
                    if (conflictsSection.DeploymentVersionNumber < latestDeploymentNumber)
                    {
                        // soft delete the outdated section
                        conflictsSection.SetDeleted(true);
                        await SaveSectionAsync(conflictsSection);
                        sections.Remove(conflictsSection);
                    }
                }
            }

            return sections;
        }

        public async Task SaveSectionAsync(Section section)
        {
            await _sectionPersistence.UpsertAsync(section.Id, section);
        }

        public async Task SaveSectionAndDraftAsync(Section section, Draft newDraft)
        {
            await _draftRepository.SaveAsync(newDraft);
            await SaveSectionAsync(section);
        }

        public async Task SaveSectionWithNewDivisionsAsync(Section section)
        {
            foreach (var sectionReference in section.References)
            {
                await _sectionReferenceAudioRepository.SaveAsync(sectionReference);
            }

            await SaveSectionAsync(section);
        }

        public async Task DeleteDraftAsync(Guid draftId)
        {
            var draftDocument = await _draftRepository.GetByIdAsync(draftId);
            if (draftDocument != null)
            {
                //TODO delete back translations and their notes
                //TODO delete interpretations
                foreach (var conversation in draftDocument.Conversations)
                {
                    foreach (var message in conversation.Messages)
                    {
                        await _audioRepository.DeleteAudioByIdAsync(message.Media.AudioId);
                    }
                }

                await _draftRepository.DeleteAudioByIdAsync(draftId);
            }
        }

        private async Task LoadDrafts(Section section)
        {
            if (section == null) return;

            foreach (var passage in section.Passages)
            {
                // for backward compatibility support
                if (passage.CurrentDraftAudioId != default)
                {
                    var draft = await _draftRepository.GetByIdAsync(passage.CurrentDraftAudioId);
                    passage.ChangeCurrentDraftAudio(draft);
                }
                else
                {
                    var drafts = await _draftRepository.GetMultipleByParentIdAsync(passage.Id);
                    if (drafts is null || !drafts.Any()) continue;
                    // get the latest not deleted draft revision for the section
                    passage.ChangeCurrentDraftAudio(drafts.Where(x => x.ProjectId == section.ProjectId && !x.Deleted)
                                                          .Where(x => x.ScopeId == section.ScopeId)
                                                          .OrderByDescending(x => x.Revision)
                                                          .ThenByDescending(x => x.DateUpdated)
                                                          .FirstOrDefault());
                }
            }
        }

        private async Task<Section> GetSectionTitleAudioAsync(Section section)
        {
            var sectionTitleAudio = await _audioRepository.GetByParentIdAsync(section.Id);
            section.Title.SetAudio(sectionTitleAudio);
            section.SetPassages(section.Passages.OrderBy(x => x.PassageNumber).ToList());
            return section;
        }

        private async Task CopySectionTitle(Section oldSection, Section section)
        {
            section.Title = oldSection.Title;

            if (section.Title.Audio != null)
            {
                section.Title.Audio.ParentId = section.Id;
                await _audioRepository.SaveAsync(section.Title.Audio);
            }
        }

        public async Task<Section> GetReferencesForSectionAsync(Section section)
        {
            var sectionReferenceAudioList = await _sectionReferenceAudioRepository.GetMultipleByParentIdAsync(section.Id);
            foreach (var sectionReferenceAudio in sectionReferenceAudioList)
            {
                sectionReferenceAudio.Reference = await _referencePersistence.GetAsync(sectionReferenceAudio.ReferenceId);
            }

            section.References = section.References = sectionReferenceAudioList;

            var sectionMaterials = await _audioRepository.GetMultipleByParentIdAsync(section.Id);
            foreach (var supplementaryMaterial in section.SupplementaryMaterials)
            {
                supplementaryMaterial.Audio =
                    sectionMaterials.SingleOrDefault(x => x.Id == supplementaryMaterial.AudioId);
            }

            return await GetPassageDraftsAsync(section);
        }

        public async Task<Section> GetPassageSupplementaryMaterials(Section section, Passage passage)
        {
            var passageMaterials = await _audioRepository.GetMultipleByParentIdAsync(passage.Id);
            foreach (var supplementaryMaterial in passage.SupplementaryMaterials)
            {
                supplementaryMaterial.Audio = passageMaterials.SingleOrDefault(x => x.Id == supplementaryMaterial.AudioId);
            }

            return section;
        }

        public async Task<Section> RevertSectionToSnapshotAsync(Snapshot snapshot, List<Snapshot> snapshots, Guid loggedInUserId, Guid projectId)
        {
            var oldSection = await GetSectionWithDraftsAsync(snapshot.SectionId, true, true);
            var section = new Section(
                title: oldSection.Title,
                scopeId: oldSection.ScopeId,
                projectId: oldSection.ProjectId,
                templateId: oldSection.TemplateId,
                number: oldSection.Number,
                scriptureReference: oldSection.ScriptureReference,
                deploymentVersionNumber: oldSection.DeploymentVersionNumber);

            //Data should stay for following stages
            var stagesComplete = snapshots
                .TakeWhile(x => x.StageId != snapshot.StageId)
                .Select(x => x.StageId)
                .ToList();

            stagesComplete.Add(snapshot.StageId);

            foreach (var passage in oldSection.Passages)
            {
                var draft = passage.CurrentDraftAudio;
                if (draft == null)
                {
                    continue;
                }

                //Remove Peer Flags
                await RemoveFlagsFor(draft, stagesComplete);

                //Remove reverted back translations for the latest section draft revision
                await RemoveBackTranslations(snapshot, draft, passage);

                //Remove Community Flags
                await RemoveCommunityTestDataIfExists(draft, section, stagesComplete);
            }

            foreach (var passage in snapshot.Passages)
            {
                var draft = passage.CurrentDraftAudio;
                if (draft == null)
                {
                    continue;
                }

                //Remove Peer Flags
                await RemoveFlagsFor(draft, stagesComplete);

                //Remove reverted back translations for the original snapshot draft revision
                if (!oldSection.Passages.Select(x => x.CurrentDraftAudio.Id).Contains(draft.Id))
                {
                    await RemoveBackTranslations(snapshot, draft, passage);
                }

                //Remove Community Flags
                await RemoveCommunityTestDataIfExists(draft, section, stagesComplete);
            }

            section.SetPassages(snapshot.Passages);
            section.SetApprovedBy(snapshot.ApprovedBy, snapshot.ApprovedDate);
            section.SetCheckedBy(snapshot.CheckedBy);

            await CopySectionTitle(oldSection, section);

            var sectionReferenceAudioList = await _sectionReferenceAudioRepository.GetMultipleByParentIdAsync(oldSection.Id);
            foreach (var sectionReferenceAudio in sectionReferenceAudioList)
            {
                var referenceSnapshot = snapshot.SectionReferenceAudioSnapshots.SingleOrDefault(
                    x => x.SectionReferenceAudioId == sectionReferenceAudio.Id);
                if (referenceSnapshot != null)
                {
                    sectionReferenceAudio.SetPassageReferences(referenceSnapshot.PassageReferences);
                    sectionReferenceAudio.LockedReferenceByPassageNumbersList = referenceSnapshot.LockedReferenceByPassageNumbersList;
                    sectionReferenceAudio.ParentId = section.Id;
                }
            }

            section.References = sectionReferenceAudioList;

            await SaveSectionAsync(section);
            await SaveSectionReferencesAsync(section.References);
            oldSection.SetDeleted(true);
            await _sectionPersistence.UpsertAsync(oldSection.Id, oldSection);

            _logger.LogInfo("Section reverted", new Dictionary<string, string>
            {
                { "Section Number", oldSection.Number.ToString() },
                { "Old Section Id", oldSection.ToString() },
                { "New Section Id", section.Id.ToString() },
                { "LoggedInUserId", loggedInUserId.ToString() },
                { "ProjectId", projectId.ToString() }
            });

            return section;
        }

        public async Task<Section> RevertSectionToDefaultAsync(Section oldSection, Guid loggedInUserId, Guid projectId)
        {
            var section = new Section(
                title: oldSection.Title,
                scopeId: oldSection.ScopeId,
                projectId: oldSection.ProjectId,
                templateId: oldSection.TemplateId,
                number: oldSection.Number,
                scriptureReference: oldSection.ScriptureReference,
                deploymentVersionNumber: oldSection.DeploymentVersionNumber);

            var defaultPassages = GetPassagesBeforeDivide(oldSection);
            section.SetPassages(defaultPassages);
            foreach (var passage in section.Passages)
            {
                await DraftSoftDelete(passage);
            }
            section.SetApprovedBy(default, default);
            section.SetCheckedBy(default);

            await CopySectionTitle(oldSection, section);

            //References
            var sectionReferenceAudioList = await _sectionReferenceAudioRepository.GetMultipleByParentIdAsync(oldSection.Id);
            foreach (var sectionReferenceAudio in sectionReferenceAudioList)
            {
                sectionReferenceAudio.ParentId = section.Id;
                sectionReferenceAudio.ResetPassageReferencesWithDivisions();
                sectionReferenceAudio.LockedReferenceByPassageNumbersList.Clear();
            }

            section.References = sectionReferenceAudioList;

            //Supplementary Material
            foreach (var supplementaryMaterial in oldSection.SupplementaryMaterials)
            {
                supplementaryMaterial.Audio.ParentId = section.Id;
                await _audioRepository.SaveAsync(supplementaryMaterial.Audio);
            }

            await SaveSectionAsync(section);
            await SaveSectionReferencesAsync(section.References);
            oldSection.SetDeleted(true);
            await _sectionPersistence.UpsertAsync(oldSection.Id, oldSection);

            _logger.LogInfo("Section reverted to default", new Dictionary<string, string>
            {
                { "Section Number", oldSection.Number.ToString() },
                { "Old Section Id", oldSection.ToString() },
                { "New Section Id", section.Id.ToString() },
                { "LoggedInUserId", loggedInUserId.ToString() },
                { "ProjectId", projectId.ToString() }
            });

            return section;
        }

        private async Task DraftSoftDelete(Passage passage)
        {
            if (passage.CurrentDraftAudio is null) return;

            passage.CurrentDraftAudio.SetDeleted(true);
            await _draftRepository.SaveAsync(passage.CurrentDraftAudio);
            passage.RemoveCurrentDraftAudio();
        }

        private async Task RemoveCommunityTestDataIfExists(Draft draft, Section section, List<Guid> stagesComplete)
        {
            var communityTest = await _communityTestRepository.GetExistingCommunityTestForDraftAsync(draft);

            if (communityTest == null)
            {
                return;
            }

            await RemoveFlagsFor(communityTest, stagesComplete);
            await RemoveRetellsFor(communityTest, stagesComplete);
            await _communityTestRepository.SaveCommunityTestAsync(communityTest);
        }

        private async Task RemoveFlagsFor(CommunityTest communityTest, List<Guid> stagesComplete)
        {
            foreach (var questionToRemove in communityTest.FlagsAllStages.SelectMany(flagToRemove => flagToRemove.Questions))
            {
                foreach (var responseToRemove in questionToRemove.Responses)
                {
                    if (!stagesComplete.Contains(responseToRemove.StageId))
                    {
                        await _responseRepository.DeleteAudioByIdAsync(responseToRemove.Id);
                    }
                }

                var stagesToRemove = questionToRemove.StageIds.Except(stagesComplete).ToList();
                questionToRemove.RemoveFromStage(stagesToRemove);

                if (!questionToRemove.StageIds.Any())
                {
                    await _notableAudioRepository.DeleteAudioByIdAsync(questionToRemove.Id);
                }
            }

            communityTest.RemoveFlagsThatDoNotBelongToAnyStage();
        }

        private async Task RemoveFlagsFor(Draft draft, ICollection<Guid> stagesDone)
        {
            var conversationsToDelete = draft.Conversations
                .Where(c => !stagesDone.Contains(c.StageId));

            foreach (var message in conversationsToDelete.SelectMany(c => c.Messages))
            {
                if (message.Media.AudioId != Guid.Empty)
                {
                    await _audioRepository.DeleteAudioByIdAsync(message.Media.AudioId);
                }
            }

            draft.Conversations = draft.Conversations.Except(conversationsToDelete).ToList();

            await _draftRepository.SaveAsync(draft);
        }

        private async Task RemoveRetellsFor(CommunityTest communityTest, List<Guid> stagesComplete)
        {
            var retellsToRemove = communityTest.RetellsAllStages.Where(i => !stagesComplete.Contains(i.StageId)).ToList();

            foreach (var item in retellsToRemove)
            {
                await _communityRetellAudioRepository.DeleteAudioByIdAsync(item.Id);
                communityTest.RemoveRetell(item.Id);
            }
        }

        private async Task RemoveBackTranslations(Snapshot snapshot, Draft draft, Passage passage)
        {
            var retellId = draft.RetellBackTranslationAudio?.Id ?? Guid.Empty;
            var passageBackTranslation = snapshot.PassageBackTranslations.FirstOrDefault(x => x.PassageId == passage.Id);
            if (retellId != passageBackTranslation?.RetellBackTranslationId && retellId != Guid.Empty)
            {
                await _retellRepository.DeleteAudioByIdAsync(retellId);
            }

            foreach (var segment in draft.SegmentBackTranslationAudios.Where(segment => passageBackTranslation != null
                                                                                        && !passageBackTranslation.SegmentBackTranslationIds.Contains(segment.Id)))
            {
                await _segmentRepository.DeleteAudioByIdAsync(segment.Id);
            }
        }

        public async Task SaveSectionReferencesAsync(IEnumerable<SectionReferenceAudio> sectionReferences)
        {
            foreach (var sectionReference in sectionReferences)
            {
                await _sectionReferenceAudioRepository.SaveAsync(sectionReference);
            }
        }

        public async Task<Snapshot> GetPassageDraftsAsync(
            Snapshot snapshot,
            bool getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false)
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

                if (getRetellBackTranslations)
                {
                    await GetRetellBackTranslationsAsync(passage);
                }

                if (getSegmentBackTranslations)
                {
                    await GetSegmentBackTranslationsAsync(passage);
                }
            }

            return snapshot;
        }

        private async Task<Section> GetPassageDraftsAsync(
            Section section,
            bool getRetellBackTranslations = false,
            bool getSegmentBackTranslations = false,
            bool getCommunityTest = false)
        {
            await LoadDrafts(section);

            foreach (var passage in section.Passages)
            {
                if (passage.CurrentDraftAudio is null) continue;

                if (getRetellBackTranslations)
                {
                    await GetRetellBackTranslationsAsync(passage);
                }

                if (getSegmentBackTranslations)
                {
                    await GetSegmentBackTranslationsAsync(passage);
                }

                if (getCommunityTest)
                {
                    passage.CurrentDraftAudio.SetCommunityTest(
                        await _communityTestRepository.GetCommunityTestForDraftAsync(passage.CurrentDraftAudio));
                }
            }

            return section;
        }

        public async Task UpdateRetellsWithSnapshotInfo(Passage passage, Snapshot snapshot)
        {
            if (passage.CurrentDraftAudio is null)
            {
                return;
            }

            if (passage.CurrentDraftAudio.RetellBackTranslationAudio is not null)
            {
                passage.CurrentDraftAudio.RetellBackTranslationAudio.CorrespondedSnashotId = snapshot.Id; //1Btr
                await _retellRepository.SaveAsync(passage.CurrentDraftAudio.RetellBackTranslationAudio);

                if (passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio is not null)
                {
                    passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio.CorrespondedSnashotId = snapshot.Id;  //2Btr
                    await _retellRepository.SaveAsync(passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio);
                }
            }

            if (passage.CurrentDraftAudio.SegmentBackTranslationAudios is not null)
            {
                foreach (var segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                {
                    segment.CorrespondedSnashotId = snapshot.Id;  //1Sbtr
                    await _segmentRepository.SaveAsync(segment);

                    if (segment.RetellBackTranslationAudio is not null)
                    {
                        segment.RetellBackTranslationAudio.CorrespondedSnashotId = snapshot.Id; //2sbtr
                        await _retellRepository.SaveAsync(segment.RetellBackTranslationAudio);
                    }
                }
            }
        }

        private async Task GetRetellBackTranslationsAsync(Passage passage)
        {
            passage.CurrentDraftAudio.RetellBackTranslationAudio = await _retellRepository
                .GetByParentIdAsync(passage.CurrentDraftAudio.Id);
            //Step 2
            if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null)
            {
                passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio = await _retellRepository
                    .GetByParentIdAsync(passage.CurrentDraftAudio.RetellBackTranslationAudio.Id);
            }
        }

        private async Task GetSegmentBackTranslationsAsync(Passage passage)
        {
            var segments = await _segmentRepository.GetMultipleByParentIdAsync(passage.CurrentDraftAudio.Id);
            passage.CurrentDraftAudio.SegmentBackTranslationAudios =
                segments?.OrderBy(x => x.TimeMarkers.StartMarkerTime).ToList() ?? [];
            //Step 2
            foreach (var segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
            {
                segment.RetellBackTranslationAudio = await _retellRepository.GetByParentIdAsync(segment.Id);
            }
        }

        private List<Passage> GetPassagesBeforeDivide(Section oldSection)
        {
            if (oldSection.Passages.Any(p => p.PassageNumber.DivisionNumber > 0))
            {
                var distinctPassages = oldSection.Passages.GroupBy(p => p.PassageNumber.Number)
                                                        .Select(g => g.First())
                                                        .OrderBy(p => p.PassageNumber.Number)
                                                        .ToList();

                distinctPassages.ForEach(p => p.SetPassageNumber(new PassageNumber(p.PassageNumber.Number)));

                return distinctPassages;
            }

            return oldSection.Passages;
        }

        public void Dispose()
        {
            _sectionPersistence?.Dispose();
            _sectionReferenceAudioRepository?.Dispose();
            _draftRepository?.Dispose();
            _audioRepository?.Dispose();
            _retellRepository?.Dispose();
            _segmentRepository?.Dispose();
            _notableAudioRepository?.Dispose();
            _responseRepository?.Dispose();
            _communityRetellAudioRepository?.Dispose();
            _referencePersistence?.Dispose();
            _communityTestRepository?.Dispose();
        }
    }
}