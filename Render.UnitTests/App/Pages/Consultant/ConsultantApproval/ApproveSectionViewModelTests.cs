using System.Reactive;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.AppStart.Home;
using Render.Pages.Consultant.ConsultantApproval;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioServices;
using Render.Services.SessionStateServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Consultant.ConsultantApproval
{
    public class ApproveSectionViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly User _user;
        private readonly Stage _stage;

        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();
        private readonly Mock<IDataPersistence<RenderProjectStatistics>> _mockStatisticsRepository = new();
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Section _section1 = Section.UnitTestEmptySection();
        private readonly Section _section2 = Section.UnitTestEmptySection();
        private readonly Guid _projectId;
        private readonly RenderUser _user1 = new("User1", Guid.Empty);
        private readonly RenderUser _user2 = new("User2", Guid.Empty);

        private readonly Mock<IDataPersistence<WorkflowStatus>> _mockWorkflowStatusRepository;
        private readonly Mock<ISnapshotRepository> _mockSnapshotRepository;
        private WorkflowStatus _workflowStatus;

        public ApproveSectionViewModelTests()
        {
            var mockModalService = new Mock<IModalService>();
            _mockWorkflowStatusRepository = new Mock<IDataPersistence<WorkflowStatus>>();
            MockContextProvider.Setup(x => x.GetPersistence<WorkflowStatus>())
                .Returns(_mockWorkflowStatusRepository.Object);
            _mockSnapshotRepository = new Mock<ISnapshotRepository>();
            MockContextProvider.Setup(x => x.GetModalService()).Returns(mockModalService.Object);
            _user = new User("Consultant User", "consultant");
            _section = Section.UnitTestEmptySection();
            _projectId = _section.ProjectId;
            _stage = new Stage();
            _stage.SetName("Stage");     

            var mockAudioPlayerService = new Mock<IAudioPlayerService>();
            mockAudioPlayerService.Setup(x => x.Duration).Returns(50);
            mockAudioPlayerService.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);

            MockContextProvider.Setup(x => x.GetAudioPlayerService(It.IsAny<Action>()))
                .Returns(mockAudioPlayerService.Object);
            var mockBarPlayerViewModel = new Mock<IBarPlayerViewModel>();
            mockBarPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            var mockMiniPlayerViewModel = new Mock<IMiniWaveformPlayerViewModel>();
            mockMiniPlayerViewModel.Setup(x => x.AudioPlayerState).Returns(AudioPlayerState.Playing);
            
            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                It.IsAny<Audio>(), ActionState.Optional,It.IsAny<string>(), 0, null, null,
                It.IsAny<ImageSource>(), It.IsAny<ReactiveCommand<Unit,IRoutableViewModel>>(), 
                It.IsAny<IObservable<bool>>(), It.IsAny<string>()))
                .Returns(mockBarPlayerViewModel.Object);
            MockContextProvider.Setup(x => x.GetMiniWaveformPlayerViewModel(It.IsAny<Audio>(), ActionState.Optional,
                    It.IsAny<string>(), It.IsAny<TimeMarkers>(), null, null, null, It.IsAny<bool>(),
                    It.IsAny<string>()))
                .Returns(mockMiniPlayerViewModel.Object);
            
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(_user);
            MockContextProvider.Setup(x => x.GetCurrentDeviceIdiom());
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(new Mock<ISessionStateService>().Object);
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(_mockSectionRepository.Object);
            var mockAudioEncodingService = new Mock<IAudioEncodingService>();

            MockContextProvider.Setup(x => x.GetAudioEncodingService()).Returns(mockAudioEncodingService.Object);
            
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().First(), _user1.Id);
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().Last(), _user2.Id);
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(_renderWorkflow);
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>{_section1, _section2});
            _mockSectionRepository.Setup(x => x.GetSectionWithDraftsAsync(It.IsAny<Guid>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(_section);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user1.Id)).ReturnsAsync(_user1);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user2.Id)).ReturnsAsync(_user2);
            
            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(_mockUserRepository.Object);
            MockGrandCentralStation.Setup(x => x.GetStepToWorkAsync(It.IsAny<Guid>(), It.IsAny<RenderStepTypes>()))
                .ReturnsAsync(new Step());
            MockContextProvider.Setup(x => x.GetPersistence<RenderProjectStatistics>()).Returns(_mockStatisticsRepository.Object);
            _mockStatisticsRepository
                .Setup(x => x.QueryOnFieldAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<RenderProjectStatistics>());
            
            var mockSequencerFactory = new Mock<ISequencerFactory>();
            mockSequencerFactory.Setup(x => x.CreatePlayer(
                It.IsAny<Func<IAudioPlayer>>(),
                It.IsAny<FlagType>()
            )).Returns(new Mock<ISequencerPlayerViewModel>().Object);
            MockContextProvider.Setup(x => x.GetSequencerFactory()).Returns(mockSequencerFactory.Object);
            
            var mockTempAudioService = new Mock<ITempAudioService>();
            MockContextProvider.Setup(x => x.GetTempAudioService(It.IsAny<Audio>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(mockTempAudioService.Object);
            MockContextProvider.Setup(x => x.GetTempAudioService(It.IsAny<AudioPlayback>())).Returns(mockTempAudioService.Object);
            mockTempAudioService.Setup(s => s.SaveTempAudio()).Returns("temp.wav");
        }
        
        [Fact]
        public async Task CreateAsync_Succeed()
        {
            //Arrange
        
            //Act
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            //Assert
            vm.BackTranslatePlayers.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task CreateAsync_With_OneBarPlayer_Succeed()
        {
            //Arrange
        
            //Act
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            //Assert
            vm.BackTranslatePlayers.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task BackTranslation_AudioNull()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
                passage.CurrentDraftAudio.RetellBackTranslationAudio = null;
            }
            
            //Act
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            //Assert
            vm.BackTranslatePlayers.Should().BeEmpty();
        }
        
        [Fact]
        public async Task TryApprove_Fails()
        {
            //Arrange
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);

            //Act
            vm.ApproveCommand.Execute().Subscribe();
            
            //Assert
            vm.Section.ApprovedBy.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task TryApprove_Succeeds()
        {
            //Arrange
            var mockSectionRepository = new Mock<ISectionRepository>();
            MockGrandCentralStation.Setup(x => x.StepsAssignedToUser())
                .Returns(new List<Guid>());
            MockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>());
            var hasher = new PasswordHasher<User>();
            var password = "hashedpassword";
            _user.HashedPassword = hasher.HashPassword(_user, password);
            var mockScopeRepo = new Mock<IDataPersistence<Scope>>();
            var mockSectionRepo = new Mock<IDataPersistence<Section>>();
            var scope = new Scope(_projectId);
            var defaultDate = DateTimeOffset.MinValue;
            var nowDate = DateTimeOffset.Now;
            scope.SetFirstSectionDraftedDate(nowDate);
            scope.SetFinalSectionApprovedDate(defaultDate);
            mockScopeRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(scope);
            mockSectionRepo.Setup(x =>
                    x.QueryOnFieldAsync("ScopeId", It.IsAny<string>(), It.IsAny<int>(), true, false))
                .ReturnsAsync(new List<Section> { _section });
            MockContextProvider.Setup(x => x.GetPersistence<Scope>()).Returns(mockScopeRepo.Object);
            MockContextProvider.Setup(x => x.GetPersistence<Section>()).Returns(mockSectionRepo.Object);
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            SetupViewModelForNavigationTest(vm);
            
            //Home screen
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
            
            mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>() { _section });
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            
            var selectSectionToApproveViewModel =
                await SelectSectionToApproveViewModel.CreateAsync(MockContextProvider.Object, new Step(), _stage);
            var homeViewModel = await HomeViewModel.CreateAsync(_section.ProjectId, MockContextProvider.Object);
            vm.HostScreen.Router.NavigationStack.Add(selectSectionToApproveViewModel);
            vm.HostScreen.Router.NavigationStack.Add(homeViewModel);
            
            //Act
            vm.ApproveCommand.Execute().Subscribe();
            await vm.NavigateToSelectApprove();

            //Assert
            vm.Section.ApprovedBy.Should().Be(_user.Id);
            MockGrandCentralStation.Verify(x => x.AdvanceSectionAsync(It.IsAny<Section>(), 
                It.IsAny<Step>()), Times.Once);
            mockSectionRepository.Verify(x => x.SaveSectionAsync(_section), Times.Once);
        }
        
        [Fact]
        public async Task TryApprove_SetScopeEndDate_Succeeds()
        {
            //Arrange
            var mockSectionRepository = new Mock<ISectionRepository>();
            MockGrandCentralStation.Setup(x => x.StepsAssignedToUser())
                .Returns(new List<Guid>());
            MockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>());
            var hasher = new PasswordHasher<User>();
            var password = "hashedpassword";
            _user.HashedPassword = hasher.HashPassword(_user, password);
            var mockScopeRepo = new Mock<IDataPersistence<Scope>>();
            var mockSectionRepo = new Mock<IDataPersistence<Section>>();
            var scope = new Scope(_projectId);
            var defaultDate = DateTimeOffset.MinValue;
            var nowDate = DateTimeOffset.Now;
            scope.SetFirstSectionDraftedDate(nowDate);
            scope.SetFinalSectionApprovedDate(defaultDate);
            mockScopeRepo.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(scope);
            mockSectionRepo.Setup(x =>
                    x.QueryOnFieldAsync("ScopeId", It.IsAny<string>(), It.IsAny<int>(), true, false))
                .ReturnsAsync(new List<Section> { _section });
            MockContextProvider.Setup(x => x.GetPersistence<Scope>()).Returns(mockScopeRepo.Object);
            MockContextProvider.Setup(x => x.GetPersistence<Section>()).Returns(mockSectionRepo.Object);
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            SetupViewModelForNavigationTest(vm);
            
            //Home screen
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
            
            mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>() { _section });
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            
            var selectSectionToApproveViewModel =
                await SelectSectionToApproveViewModel.CreateAsync(MockContextProvider.Object, new Step(), _stage);
            var homeViewModel = await HomeViewModel.CreateAsync(_section.ProjectId, MockContextProvider.Object);
            vm.HostScreen.Router.NavigationStack.Add(selectSectionToApproveViewModel);
            vm.HostScreen.Router.NavigationStack.Add(homeViewModel);
            
            //Act
            vm.ApproveCommand.Execute().Subscribe();
            await vm.NavigateToSelectApprove();

            //Assert
            scope.FinalSectionApprovedDate.Year.Should().Be(nowDate.Year);
            scope.FinalSectionApprovedDate.Month.Should().Be(nowDate.Month);
            scope.FinalSectionApprovedDate.Day.Should().Be(nowDate.Day);
            mockScopeRepo.Verify(x => x.UpsertAsync(scope.Id, scope), Times.Once);
        }
        
        [Fact]
        public async Task ReturnSection_ConsultantRevise__Succeeds()
        {
            //Arrange
            WorkflowStatus newWorkflowStatus = new WorkflowStatus(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, RenderStepTypes.NotSpecial);
            var workflow = RenderWorkflow.Create(_projectId);
            var consultantStage = ConsultantCheckStage.Create();
            workflow.AddStage(consultantStage);
            MockGrandCentralStation.Setup(x => x.ProjectWorkflow)
                .Returns(workflow);
            MockGrandCentralStation
                .Setup(x => x.GetStepToWorkAsync(It.IsAny<Guid>(), RenderStepTypes.ConsultantApproval))
                .ReturnsAsync(workflow.ApprovalStage.Steps.First);
            _workflowStatus = new WorkflowStatus(_section.Id, workflow.Id, _projectId, 
                workflow.ApprovalStage.Steps.FirstOrDefault().Id, default, workflow.ApprovalStage.Id, 
                RenderStepTypes.ConsultantApproval);
            _mockWorkflowStatusRepository.Setup(x => x.QueryOnFieldAsync("ParentSectionId",
                    It.IsAny<string>(), 0, true, false))
                .ReturnsAsync(new List<WorkflowStatus> {_workflowStatus});
            _mockWorkflowRepository.Setup(x => x.GetAllWorkflowsForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<RenderWorkflow> { workflow });
            var snapshotList = new List<Snapshot>();
            snapshotList.Add(new Snapshot(_section.Id, Guid.Empty, Guid.Empty, _section.ApprovedDate, Guid.Empty, Guid.Empty, Guid.Empty,
                consultantStage.Id, _workflowStatus.CurrentStepId, new List<Passage>(),
                new List<SectionReferenceAudioSnapshot>(), new List<Guid>(), "", Guid.Empty));
            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);
            MockContextProvider.Setup(x => x.GetSnapshotRepository()).Returns(_mockSnapshotRepository.Object);
            _mockWorkflowStatusRepository.Setup(x => x.UpsertAsync(It.IsAny<Guid>(), It.IsAny<WorkflowStatus>()))
                .Callback<Guid, WorkflowStatus>((_, workflowStatus) => newWorkflowStatus = workflowStatus);
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            //Act
            SetupViewModelForNavigationTest(vm);
            SetupMocksForHomeViewModelNavigation();
            // SetupMocksForHomeViewModelNavigation method setups mocked GrandCentralStation with mocked workflow
            // this is to override mocked GrandCentralStation with actual workflow for testing
            MockContextProvider.Setup(x => x.GetGrandCentralStation()).Returns(MockGrandCentralStation.Object);
            await vm.ReturnSectionBackAndGoHOme();

            //Assert
            _mockWorkflowStatusRepository.Verify(x => x.UpsertAsync(_workflowStatus.Id, _workflowStatus), Times.Once);
            _mockWorkflowStatusRepository.Verify(x => x.UpsertAsync(It.IsAny<Guid>(), It.IsAny<WorkflowStatus>()), Times.Exactly(2));
            _mockSnapshotRepository.Verify(x => x.BatchSoftDeleteAsync(snapshotList, It.IsAny<Section>()));
            newWorkflowStatus.CurrentStepType.Should().Be(RenderStepTypes.ConsultantRevise);
        }
        
        [Fact]
        public async Task ReturnSection_DraftStep__Succeeds()
        {
            //Arrange
            WorkflowStatus newWorkflowStatus = new WorkflowStatus(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, RenderStepTypes.NotSpecial);
            var workflow = RenderWorkflow.Create(_projectId);
            MockGrandCentralStation.Setup(x => x.ProjectWorkflow)
                .Returns(workflow);
            MockGrandCentralStation
                .Setup(x => x.GetStepToWorkAsync(It.IsAny<Guid>(), RenderStepTypes.ConsultantApproval))
                .ReturnsAsync(workflow.ApprovalStage.Steps.First);
            _workflowStatus = new WorkflowStatus(_section.Id, workflow.Id, _projectId, 
                workflow.ApprovalStage.Steps.FirstOrDefault().Id, default, workflow.ApprovalStage.Id, 
                RenderStepTypes.ConsultantApproval);
            _mockWorkflowStatusRepository.Setup(x => x.QueryOnFieldAsync("ParentSectionId",
                    It.IsAny<string>(), 0, true, false))
                .ReturnsAsync(new List<WorkflowStatus> {_workflowStatus});
            _mockWorkflowRepository.Setup(x => x.GetAllWorkflowsForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<RenderWorkflow> { workflow });
            var snapshotList = new List<Snapshot>();
            snapshotList.Add(new Snapshot(_section.Id, Guid.Empty, Guid.Empty, _section.ApprovedDate, Guid.Empty, Guid.Empty, Guid.Empty,
                workflow.DraftingStage.Id, _workflowStatus.CurrentStepId, new List<Passage>(),
                new List<SectionReferenceAudioSnapshot>(), new List<Guid>(), "", Guid.Empty));
            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);
            MockContextProvider.Setup(x => x.GetSnapshotRepository()).Returns(_mockSnapshotRepository.Object);
            _mockWorkflowStatusRepository.Setup(x => x.UpsertAsync(It.IsAny<Guid>(), It.IsAny<WorkflowStatus>()))
                .Callback<Guid, WorkflowStatus>((_, workflowStatus) => newWorkflowStatus = workflowStatus);
            var vm = await ApproveSectionViewModel.CreateAsync(MockContextProvider.Object, _section.Id,
                null);
            
            //Act
            SetupViewModelForNavigationTest(vm);
            SetupMocksForHomeViewModelNavigation();
            // SetupMocksForHomeViewModelNavigation method setups mocked GrandCentralStation with mocked workflow
            // this is to override mocked GrandCentralStation with actual workflow for testing
            MockContextProvider.Setup(x => x.GetGrandCentralStation()).Returns(MockGrandCentralStation.Object);
            await vm.ReturnSectionBackAndGoHOme();

            //Assert
            _mockWorkflowStatusRepository.Verify(x => x.UpsertAsync(_workflowStatus.Id, _workflowStatus), Times.Once);
            _mockWorkflowStatusRepository.Verify(x => x.UpsertAsync(It.IsAny<Guid>(), It.IsAny<WorkflowStatus>()),
                Times.Exactly(2));
            _mockSnapshotRepository.Verify(x => x.BatchSoftDeleteAsync(snapshotList, It.IsAny<Section>()));
            newWorkflowStatus.CurrentStepType.Should().Be(RenderStepTypes.Draft);
        }
    }
}