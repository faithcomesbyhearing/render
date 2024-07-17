using Render.Models.Workflow;

namespace Render.Repositories.WorkflowRepositories
{
    public interface IWorkflowRepository : IDisposable
    {
        Task<RenderWorkflow> GetWorkflowForProjectIdAsync(Guid projectId);

        Task SaveWorkflowAsync(RenderWorkflow renderWorkflow);

        Task<List<RenderWorkflow>> GetAllWorkflowsForProjectIdAsync(Guid projectId);

        Task Purge(Guid id);
    }
}