using Moq;
using Render.Kernel;
using Render.Models.Users;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;

namespace Render.UnitTests.App.Kernel;

public class ProjectDownloadServiceTest : TestBase
{
    private readonly IProjectDownloadService _projectDownloadService;
    private readonly Mock<IOneShotReplicator> _mockOneShotReplicator;
    private readonly Project _project;
    private readonly List<Guid> _globalUsersIds;
    private Mock<IViewModelContextProvider> MockContextProvider { get; }

    public ProjectDownloadServiceTest()
    {
        var user = new User("test", "test");
        _globalUsersIds = new List<Guid>() { user.Id };
        _project = new Project("test", "1:10-1", "123");
        _mockOneShotReplicator = new Mock<IOneShotReplicator>();
        MockContextProvider = new Mock<IViewModelContextProvider>();
        MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(user);
        MockContextProvider.Setup(x => x.GetOneShotReplicator(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
            It.IsAny<string>(), It.IsAny<string>())).Returns(_mockOneShotReplicator.Object).Verifiable();

        _projectDownloadService = new ProjectDownloadService(MockContextProvider.Object);
    }

    [Fact]
    public void StartDownload_WhenUserClickedDownload_ShouldCreateAReplicatorAtLeastOnce()
    {
        //Arrange
        //Act
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);

        //Assert
        MockContextProvider.Verify(x => x.GetOneShotReplicator(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void StartDownload_WhenUserClickedDownloadButtonTwoTimes_ShouldCreateAReplicatorOnlyOnce()
    {
        //Arrange
        //Act
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);

        //Assert
        MockContextProvider.Verify(x => x.GetOneShotReplicator(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void StartDownload_WhenProjectSuccessfullyDownloaded_ReplicatorShouldBeDisposed()
    {
        //Arrange
        //Act
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);
        _mockOneShotReplicator.Raise(x => x.DownloadFinished += null, true);

        //Assert
        MockContextProvider.Verify(x => x.GetOneShotReplicator(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        _mockOneShotReplicator.Verify(x => x.Dispose(), Times.AtLeastOnce);
    }

    [Fact]
    public void ResumeDownload_WhenUserClickedDownload_ShouldCreateAReplicatorAtLeastOnce()
    {
        //Arrange
        //Act
        _projectDownloadService.ResumeDownload(_project.Id, _globalUsersIds, default, default);

        //Assert
        MockContextProvider.Verify(x => x.GetOneShotReplicator(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void StopDownload_WhenProjectDownloadIsCanceled_ReplicatorShouldBeDisposed()
    {
        //Arrange
        //Act
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);
        _projectDownloadService.StopDownload(_project.Id);

        //Assert
        _mockOneShotReplicator.Verify(x => x.Dispose(), Times.AtLeastOnce);
    }

    [Fact]
    public void StopDownload_WhenProjectDownloadFinished_CompletedFlagIsTrue()
    {
        //Arrange
        bool? downloadCompleted = null;
        
        _projectDownloadService.StartDownload(_project.Id, _globalUsersIds, default, default);
        _projectDownloadService.WhenDownloadFinished(_project.Id).Subscribe(completed => { downloadCompleted = completed; });

        //Act
        _mockOneShotReplicator.Raise(x => x.DownloadFinished += null, true);

        //Assert
        downloadCompleted.Should().NotBeNull();
        downloadCompleted.Should().BeTrue();
    }
    
   
    [Fact]
    public void WhenDownloadFinished_ProjectIdIsUnknown_CompletedFlagIsFalse()
    {
        //Arrange
        bool? downloadCompleted = null;

        //Act
        _projectDownloadService.WhenDownloadFinished(Guid.NewGuid())
            .Subscribe(completed => { downloadCompleted = completed; });

        //Assert
        downloadCompleted.Should().NotBeNull();
        downloadCompleted.Should().BeFalse();
    }
}