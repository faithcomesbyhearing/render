using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Repositories.Audio;
using Render.Repositories.LocalDataRepositories;

namespace Render.Services.SessionStateServices
{
    public class SessionStateService : ISessionStateService
    {
        private readonly ISessionStateRepository _sessionStateRepository;
        private readonly IAudioRepository<Audio> _audioRepository;

        private List<UserProjectSession> AllSessions { get; set; } = new ();

        private Guid UserId { get; set; }

        private Guid ProjectId { get; set; }

        public UserProjectSession ActiveSession { get; set; }

        public List<Guid> AudioIds => ActiveSession?.DraftAudioIds;

        public SessionStateService(
            ISessionStateRepository sessionStateRepository,
            IAudioRepository<Audio> audioRepository)
        {
            _sessionStateRepository = sessionStateRepository;
            _audioRepository = audioRepository;
        }

        public void AddDraftAudio(Audio audio)
        {
            if (!ActiveSession.AddDraftAudioId(audio.Id) || audio is Draft)
            {
                return;
            }
            
            Task.Factory.StartNew(() => { _ = _audioRepository.SaveAsync(audio); });
            SaveSession();
        }

        public void RemoveDraftAudio(Guid audioId)
        {
            Task.Factory.StartNew(() => { _ = _audioRepository.DeleteAudioByIdAsync(audioId); });
            ActiveSession.RemoveDraftAudioId(audioId);
            SaveSession();
        }
        
        public async Task RemoveDraftAudios(IEnumerable<Guid> audioIds)
        {
            foreach (var audioId in audioIds.ToList())
            {
                await _audioRepository.DeleteAudioByIdAsync(audioId);
                ActiveSession.RemoveDraftAudioId(audioId);
            }
            
            await SaveSessionAsync();
        }

        public void AddRequirementCompletion(Guid completedRequirementId)
        {
            ActiveSession.AddRequirementCompletion(completedRequirementId);
            SaveSession();
        }

        public async Task LoadUserProjectSessionAsync(Guid userId, Guid projectId)
        {
            UserId = userId;
            ProjectId = projectId;
            AllSessions = await _sessionStateRepository.GetUserProjectSessionAsync(userId, projectId);
        }

        public void StartPage(Guid sectionId, string pageName, PassageNumber passageNumber, Guid nonDraftTranslationId, Step step = null)
        {
            if (ActiveSession == null)
            {
                return;
            }

            //If we're going to the page because it's in the session, ignore this attempt
            if (ActiveSession.SectionId == sectionId && 
                ActiveSession.PageName == pageName && 
                PassageNumber.BothAreNullOrEqual(ActiveSession.PassageNumber, passageNumber) && 
                ActiveSession.NonDraftTranslationId == nonDraftTranslationId)
            {
                return;
            }

            //If we're just going to the next page in the current step/section/passage
            //(e.g. going from drafting to draft select) then just update the page name on the session
            if (ActiveSession.SectionId == sectionId && 
                PassageNumber.BothAreNullOrEqual(ActiveSession.PassageNumber, passageNumber) && 
                ActiveSession.NonDraftTranslationId == nonDraftTranslationId)
            {
                ActiveSession.SetPageName(pageName);
                SaveSession();
                return;
            }
            
            //If we're starting a new step/section/passage, then delete current drafts and reset the whole thing
            var audioIds = AudioIds.ToList();
            Task.Run(async () => await DeleteAllTemporaryAudioAsync(audioIds));
            ActiveSession.SetPassageNumber(passageNumber);
            ActiveSession.SetNonDraftTranslationId(nonDraftTranslationId);
            ActiveSession.StartStep(sectionId, pageName);
            SaveSession();
        }

        public bool RequirementMetInSession(Guid requirementId)
        {
            return ActiveSession?.RequirementMetInSession(requirementId) ?? false;
        }

        public void SetCurrentStep(Guid stepId, Guid sectionId)
        {
            ActiveSession = AllSessions.FirstOrDefault(x => x.StepId == stepId);

            if (ActiveSession == null)
            {
                ActiveSession = new UserProjectSession(UserId, ProjectId, stepId);
                AllSessions.Add(ActiveSession);
                SaveSession();
            }

            if (ActiveSession.SectionId != sectionId)
            {
                ActiveSession.Reset();
            }
        }
        
        public async Task ResetPassageNumberForSectionStep(Guid stepId, Guid sectionId)
        {
            var session = AllSessions.FirstOrDefault(x => x.StepId == stepId && x.SectionId == sectionId);

            if (session != null)
            {
                session.SetPassageNumber(null);

                await SaveSessionAsync(session);
            }
        }

        public string GetSessionPage(Guid stepId, Guid sectionId, PassageNumber passageNumber)
        {
            ActiveSession = AllSessions.FirstOrDefault(x => x.StepId == stepId);

            if (ActiveSession == null)
            {
                ActiveSession = new UserProjectSession(UserId, ProjectId, stepId);
                AllSessions.Add(ActiveSession);
                SaveSession();
            }

            return ActiveSession.CheckForCurrentStep(stepId, sectionId, passageNumber);
        }

        public async Task ResetSessionAsync()
        {
            if (AudioIds != null)
            {
                await DeleteAllTemporaryAudioAsync(AudioIds);
            }

            foreach (var session in AllSessions)
            {
                session.Reset();
                await _sessionStateRepository.SaveSessionStateAsync(session);
            }
        }

        public async Task FinishSessionAsync()
        {
            if (ActiveSession != null)
            {
                ActiveSession.Reset();
                await _sessionStateRepository.SaveSessionStateAsync(ActiveSession);
            }
        }

        public void SetCurrentDraftId(Guid id)
        {
            if (ActiveSession == null)
            {
                return;
            }

            ActiveSession.CurrentDraftAudioId = id;
            SaveSession();
        }

        public async Task DeleteAllTemporaryAudioAsync(List<Guid> ids)
        {
            foreach (var audioId in ids)
            {
                await _audioRepository.DeleteAudioByIdAsync(audioId);
            }
        }

        private void SaveSession()
        {
            Task.Factory.StartNew(() => { _ = SaveSessionAsync(); });
        }
        
        private async Task SaveSessionAsync()
        {
            await _sessionStateRepository.SaveSessionStateAsync(ActiveSession);
        }
        
        private async Task SaveSessionAsync(UserProjectSession session)
        {
            await _sessionStateRepository.SaveSessionStateAsync(session);
        }
    }
}