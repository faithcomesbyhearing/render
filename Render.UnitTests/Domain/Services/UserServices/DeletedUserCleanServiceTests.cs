using Moq;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services.UserServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.UserServices;

public class DeletedUserCleanServiceTests : ViewModelTestBase
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private DeletedUserCleanService _deletedUserCleanService;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly RenderUser _user1 = new("User1", Guid.NewGuid());
    private readonly RenderUser _user2 = new("User2", Guid.NewGuid());

    [Fact]
    public async Task Clean_UserIsNotDeleted_DoesNothing()
    {
        // Arrange
        _mockUserRepository.Setup(repo => repo.GetUserAsync(_userId))
            .ReturnsAsync(_user1);
        _deletedUserCleanService =
            new DeletedUserCleanService(_mockUserRepository.Object, _mockWorkflowRepository.Object, _projectId);
        // Act
        await _deletedUserCleanService.Clean(_userId);

        // Assert
        _mockWorkflowRepository.Verify(repo => repo.GetAllWorkflowsForProjectIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockWorkflowRepository.Verify(repo => repo.SaveWorkflowAsync(It.IsAny<RenderWorkflow>()), Times.Never);
    }

    [Fact]
    public async Task Clean_UserNotFound_RemovesAssignmentsFromTeams()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetUserAsync(_userId)).ReturnsAsync((User)null);
        RenderWorkflow renderWorkflow = RenderWorkflow.Create(_projectId);
        renderWorkflow.AddTeam();
        renderWorkflow.AddTranslationAssignmentForTeam(renderWorkflow.GetTeams().First(), _user1.Id);
        var stage = new Stage();
        renderWorkflow.AddStage(stage);
        renderWorkflow.AddWorkflowAssignmentToTeam(stage.Id, Roles.Approval, _user2,
            renderWorkflow.GetTeams().FirstOrDefault());

        _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(_projectId))
            .ReturnsAsync(renderWorkflow);
        _mockWorkflowRepository.Setup(x => x.GetAllWorkflowsForProjectIdAsync(_projectId))
            .ReturnsAsync([renderWorkflow]);

        _deletedUserCleanService =
            new DeletedUserCleanService(_mockUserRepository.Object, _mockWorkflowRepository.Object, _projectId);

        // Act
        await _deletedUserCleanService.Clean(_user1.Id);
        await _deletedUserCleanService.Clean(_user2.Id);

        // Assert
        Assert.True(renderWorkflow.GetTeams().First().TranslatorId == Guid.Empty);
        Assert.True(renderWorkflow.GetTeams().First().WorkflowAssignments.Count(x => x.UserId == _user2.Id) == 0);
    }
}