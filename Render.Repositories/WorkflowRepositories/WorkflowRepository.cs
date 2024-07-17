using Render.Models.Workflow;
using Render.Repositories.Kernel;

namespace Render.Repositories.WorkflowRepositories
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly IDataPersistence<RenderWorkflow> _workflowPersistence;

        public WorkflowRepository(IDataPersistence<RenderWorkflow> workflowPersistence)
        {
            _workflowPersistence = workflowPersistence;
        }

        public async Task<RenderWorkflow> GetWorkflowForProjectIdAsync(Guid projectId)
        {
            var workflow = (await _workflowPersistence.QueryOnFieldAsync(
                searchField: "ProjectId",
                value: projectId.ToString(),
                limit: 0)).SingleOrDefault(x => x.ParentWorkflowId == Guid.Empty);

            return workflow ?? RenderWorkflow.Create(projectId);
        }

        public async Task SaveWorkflowAsync(RenderWorkflow renderWorkflow)
        {
            await _workflowPersistence.UpsertAsync(renderWorkflow.Id, renderWorkflow);
        }
        
        public async Task<List<RenderWorkflow>> GetAllWorkflowsForProjectIdAsync(Guid projectId)
        {
            var workflows = await _workflowPersistence.QueryOnFieldAsync(
                searchField: "ProjectId",
                value: projectId.ToString(),
                limit: 0);

            return workflows ?? new List<RenderWorkflow>();
        }
        
        public async Task Purge(Guid id)
        { 
            await _workflowPersistence.PurgeAllOfTypeForProjectId(id);
        }

        public void Dispose()
        {
            _workflowPersistence?.Dispose();
        }
    }
}