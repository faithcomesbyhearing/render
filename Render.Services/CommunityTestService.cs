using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.SectionRepository;

namespace Render.Services
{
    public class CommunityTestService : ICommunityTestService
    {
        private readonly ICommunityTestRepository _communityTestRepository;
        private readonly ISectionRepository _sectionRepository;

        public CommunityTestService(ICommunityTestRepository communityTestRepository, ISectionRepository sectionRepository)
        {
            _communityTestRepository = communityTestRepository;
            _sectionRepository = sectionRepository;
        }
        
        public CommunityTestForStage GetCommunityTestForStage(
            Stage stage, 
            CommunityTest communityTest, 
            IEnumerable<Stage> workflowStages)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (communityTest == null) throw new ArgumentNullException(nameof(communityTest));
            if (workflowStages == null) throw new ArgumentNullException(nameof(workflowStages));

            var consecutiveStages = new List<Guid> { stage.Id };
            consecutiveStages.AddRange(GetPreviousConsecutiveStages(workflowStages, stage.Id));

			var flags = new List<Flag>();
			// We need to display all flags from previous community test stages for community revise
			foreach (var stageId in consecutiveStages)
			{
				flags.AddRange(communityTest.GetFlags(stageId));
			}

			var retells = communityTest.RetellsAllStages
                .Where(retell => consecutiveStages.Contains(retell.StageId))
                .ToList()
                .AsReadOnly();

			var responses = communityTest.ResponsesAllStages
				.Where(response => consecutiveStages.Contains(response.StageId))
				.ToList()
				.AsReadOnly();

			return new CommunityTestForStage(stage.Id, flags, retells, responses);
        }
        
        // Applies only to multiple consecutive community stages
        public async Task CopyFlagsAsync(Guid fromStageId, Guid toStageId, Guid sectionId)
        {
            if (sectionId == default) throw new ArgumentException("Parameter should not be an empty GUID", nameof(sectionId));
            if (fromStageId == default) throw new ArgumentException("Parameter should not be an empty GUID", nameof(fromStageId));
            if (toStageId == default) throw new ArgumentException("Parameter should not be an empty GUID", nameof(toStageId));
            
            var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, getCommunityTest: true);

            foreach (var passage in section.Passages.Where(x => x.CurrentDraftAudio.HasCommunityTest))
            {
                var communityTest = passage.CurrentDraftAudio.GetCommunityCheck();
                
                communityTest.CopyFlags(fromStageId, toStageId);
                await _communityTestRepository.SaveCommunityTestAsync(communityTest);
            }
        }

        public IEnumerable<Guid> GetPreviousConsecutiveStages(IEnumerable<Stage> workflowStages, Guid stageId)
        {
            if (stageId == default) throw new ArgumentException("Parameter should not be an empty GUID", nameof(stageId));
            if (workflowStages == null) throw new ArgumentNullException(nameof(workflowStages));

            var workflowStagesList = workflowStages.ToList();
            
            if (!workflowStagesList.Any()) throw new ArgumentException("Parameter should not be an empty collection", nameof(workflowStages));

            var stage = workflowStagesList.FirstOrDefault(x => x.Id == stageId);
            
            if (stage == null) throw new Exception($"{nameof(workflowStages)} collection does not contain '{stageId}' stage");
            
            var result = new List<Guid>();
            for (int i = workflowStagesList.IndexOf(stage) - 1; i >= 0; i--)
            {
                var item = workflowStagesList.ElementAt(i);

                if (item.StageType == StageTypes.CommunityTest)
                {
                    result.Add(item.Id);
                }
                else
                {
                    return result;
                }
            }

            return result;
        }
    }
}