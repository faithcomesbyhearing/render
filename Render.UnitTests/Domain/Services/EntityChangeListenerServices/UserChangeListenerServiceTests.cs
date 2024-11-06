using Moq;
using Render.Models.Users;
using Render.Repositories.Kernel;
using Render.Services.EntityChangeListenerServices;

namespace Render.UnitTests.Domain.Services.EntityChangeListenerServices;

public class UserChangeListenerServiceTests
{
    private readonly Mock<IDocumentChangeListener> _mockDocumentChangeListener =new();
    private readonly UserChangeListenerService _userChangeListenerService;
    private readonly Func<Guid, Task> _mockAction;
    private readonly List<Guid> _userIds = [Guid.NewGuid(), Guid.NewGuid()];

    public UserChangeListenerServiceTests()
    {
        _mockAction = _ => Task.CompletedTask;
        _userChangeListenerService = new UserChangeListenerService(
            _mockDocumentChangeListener.Object, 
            _userIds);
    }

    [Fact]
    public void InitializeListener_ShouldMonitorDocumentsForAllUserIds()
    {
        // Act
        _userChangeListenerService.InitializeListener(_mockAction);

        // Assert
        foreach (var userId in _userIds)
        {
            _mockDocumentChangeListener.Verify(listener => 
                listener.MonitorDocumentByField<User>(nameof(User.Id), userId.ToString(), _mockAction), Times.Once);
        }
    }
}