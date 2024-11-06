using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow.Stage;

namespace Render.Services
{
    public interface ICommunityTestService
    {
        CommunityTestForStage GetCommunityTestForStage(
            Stage stage,
            CommunityTest communityTest,
            IEnumerable<Stage> workflowStages);

        Task CopyFlagsAsync(Guid fromStageId, Guid toStageId, Guid sectionId);
    }
}