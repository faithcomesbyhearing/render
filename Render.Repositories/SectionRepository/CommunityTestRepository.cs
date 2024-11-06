using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Repositories.Audio;
using Render.Repositories.Kernel;

namespace Render.Repositories.SectionRepository
{
    public class CommunityTestRepository : ICommunityTestRepository
    {
        private readonly IDataPersistence<CommunityTest> _communityTestPersistence;
        private readonly IAudioRepository<CommunityRetell> _communityRetellPersistence;
        private readonly IAudioRepository<NotableAudio> _notableAudioRepository;
        private readonly IAudioRepository<StandardQuestion> _standardQuestionRepository;
        private readonly IAudioRepository<Response> _responseRepository;

        public CommunityTestRepository(
            IDataPersistence<CommunityTest> communityTestPersistence,
            IAudioRepository<CommunityRetell> communityRetellPersistence,
            IAudioRepository<NotableAudio> notableAudioRepository,
            IAudioRepository<StandardQuestion> standardQuestionRepository,
            IAudioRepository<Response> responseRepository)
        {
            _communityTestPersistence = communityTestPersistence;
            _communityRetellPersistence = communityRetellPersistence;
            _notableAudioRepository = notableAudioRepository;
            _standardQuestionRepository = standardQuestionRepository;
            _responseRepository = responseRepository;
        }

        public async Task<CommunityTest> GetCommunityTestForDraftAsync(Draft draft)
        {
            var communityTest = await _communityTestPersistence.QueryOnFieldAsync("ParentId", draft.Id.ToString()) ??
                                new CommunityTest(draft.Id, draft.ScopeId, draft.ProjectId);
            
            communityTest = await InitializeCommunityTest(communityTest);
            return communityTest;
        }
        
        public async Task<CommunityTest> GetExistingCommunityTestForDraftAsync(Draft draft)
        {
            var communityTest = await _communityTestPersistence.QueryOnFieldAsync("ParentId", draft.Id.ToString());
            if (communityTest == null)
            {
                return null;
            }

            communityTest = await InitializeCommunityTest(communityTest);
            return communityTest;
        }
        
        public async Task SaveCommunityTestAsync(CommunityTest test)
        {
            await _communityTestPersistence.UpsertAsync(test.Id, test);
        }
        
        private async Task<CommunityTest> InitializeCommunityTest(CommunityTest communityTest)
        {
            communityTest = await GetQuestionAudiosForCommunityTestAsync(communityTest);
            communityTest = await GetCommunityRetellsForCommunityTestAsync(communityTest);
			if (!communityTest.FlagsAllStages.Any())
            {
				communityTest = await GetCommunityResponsesForCommunityTestAsync(communityTest);
			}

			return communityTest;
        }

        private async Task<CommunityTest> GetQuestionAudiosForCommunityTestAsync(CommunityTest test)
        {
            foreach (var flag in test.FlagsAllStages)
            {
                await GetQuestionsForFlagAsync(flag);
            }
            
            return test;
        }

        private async Task GetQuestionsForFlagAsync(Flag flag)
        {
            foreach (var question in flag.Questions)
            {
                var questionAudio = await _notableAudioRepository.GetByIdAsync(question.QuestionAudioId);
                if (questionAudio == null) break;

                var responses = await _responseRepository.GetMultipleByParentIdAsync(question.Id);
                responses = responses.Where(x => x.Id != questionAudio.Id).ToList();

                question.UpdateAudio(questionAudio);
                question.AddResponses(responses);
            }
            flag.RemoveEmptyQuestions();
            flag.QuestionCount = flag.Questions.Count;
        }

        private async Task<CommunityTest> GetCommunityRetellsForCommunityTestAsync(CommunityTest test)
        {
            var retells = await _communityRetellPersistence.GetMultipleByParentIdAsync(test.Id);
            test.AddRetells(retells);
            return test;
        }

		private async Task<CommunityTest> GetCommunityResponsesForCommunityTestAsync(CommunityTest test)
		{
			var responses = await _responseRepository.GetMultipleByParentIdAsync(test.Id);
			test.AddResponses(responses);
			return test;
		}
        
        public void Dispose()
        {
            _responseRepository?.Dispose();
            _communityRetellPersistence?.Dispose();
            _communityTestPersistence?.Dispose();
            _notableAudioRepository?.Dispose();
            _standardQuestionRepository?.Dispose();
        }
    }
}