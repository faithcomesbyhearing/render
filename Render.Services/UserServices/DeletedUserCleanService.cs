using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;

namespace Render.Services.UserServices;

public class DeletedUserCleanService(IUserRepository renderUserRepository,
    IWorkflowRepository workflowRepository, Guid projectId)
    : IDeletedUserCleanService
{
    private IWorkflowRepository WorkflowRepository { get; } = workflowRepository;
    private IUserRepository RenderUserRepository { get; } = renderUserRepository;

    public async Task Clean(Guid userId)
    {
        var user = await RenderUserRepository.GetUserAsync(userId);
        if (user == null)
        {
            var renderWorkflows = await WorkflowRepository.GetAllWorkflowsForProjectIdAsync(projectId);
            foreach (var renderWorkflow in renderWorkflows)
            {
                var teams = renderWorkflow.GetTeams();
                foreach (var team in teams)
                {
                    team.RemoveAssignments(userId);
                }
                await WorkflowRepository.SaveWorkflowAsync(renderWorkflow);
            }
        }
    }
}