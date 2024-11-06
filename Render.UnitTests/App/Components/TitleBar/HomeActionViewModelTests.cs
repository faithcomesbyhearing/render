using System.ComponentModel;
using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.AppStart.Home;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.Services.SessionStateServices;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.TitleBar
{
    public class HomeActionViewModelTests : ViewModelTestBase
    {
        private bool _activated;
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly Mock<IModalService> _mockModalService = new();
        public HomeActionViewModelTests()
        {
            MockStageService.Setup(x => x.StepsAssignedToUser()).Returns(new List<Guid>());
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(new Mock<IUser>().Object);
            
            var sessionStateService = new Mock<ISessionStateService>();
            sessionStateService.Setup(x => x.LoadUserProjectSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()));
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(sessionStateService.Object);
            MockContextProvider.Setup(x => x.GetModalService()).Returns(new Mock<IModalService>().Object);
            
            var mockSectionRepository = new Mock<ISectionRepository>();
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(mockSectionRepository.Object);
            
            var mockUserRepository = new Mock<IUserRepository>();
            var user1 = new RenderUser("User1", Guid.Empty);
            var  user2 = new RenderUser("User2", Guid.Empty);
            mockUserRepository.Setup(x => x.GetUserAsync(user1.Id)).ReturnsAsync(user1);
            mockUserRepository.Setup(x => x.GetUserAsync(user2.Id)).ReturnsAsync(user2);
            
            var renderWorkflow = RenderWorkflow.Create(Guid.Empty);
            renderWorkflow.AddTranslationAssignmentForTeam(renderWorkflow.GetTeams().First(), user1.Id);
            renderWorkflow.AddTranslationAssignmentForTeam(renderWorkflow.GetTeams().Last(), user2.Id);
            
            var mockWorkflowRepository = new Mock<IWorkflowRepository>();
            mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(renderWorkflow);
            
            mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Section>{Section.UnitTestEmptySection(), Section.UnitTestEmptySection()});

            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(mockUserRepository.Object);
            MockContextProvider.Setup(x => x.GetRenderChangeMonitoringService())
                .Returns(new Mock<IDocumentChangeListener>().Object);
            
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(_projectId)).ReturnsAsync(project);
            
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
            MockContextProvider.Setup(x => x.GetModalService()).Returns(_mockModalService.Object);
        }

        [Fact]
        public async Task Command_NavigatesHome_ResetRouter()
        {
            //Arrange
            SetupProjectId(_projectId);

            //Act
            var vm = new HomeActionViewModel(MockContextProvider.Object, string.Empty);
            SetupViewModelForNavigationTest(vm);
            
            //Assert
            vm.Should().NotBeNull();
            await VerifyNavigationResultAsync<HomeViewModel>(vm.Command);
        }

        [Fact]
        public async Task Command_NavigatesHome_SwitchToMainStack()
        {
            //Arrange
            SetupProjectId(_projectId);
            
            //Act
            var vm = new HomeActionViewModel(MockContextProvider.Object, string.Empty);
            SetupViewModelForNavigationTest(vm);
        
            //Assert
            vm.Should().NotBeNull();
            await VerifyNavigationResultAsync<HomeViewModel>(vm.Command);
        }
        
        [Fact]
        public async Task ExecutingCommand_ExecutesAndClosesModal()
        {
            //Arrange
            var command = ReactiveCommand.Create(CommandAction);
            var vm = new HomeActionViewModel( MockContextProvider.Object, "Home");
            vm.SetCommand(command);
            SetupViewModelForNavigationTest(vm);

            //Act/Assert
            await VerifyNavigationResultAsync<BogusViewModelTest>(vm.Command);
            _activated.Should().BeTrue();
        }
        
        [Fact]
        public async Task ExecutingCommand_AddedFalseCondition_ActionIsNotExecuted()
        {
            //Arrange
            var vm = new HomeActionViewModel( MockContextProvider.Object, "Home");
            SetupViewModelForNavigationTest(vm);

            Task<bool> FalseCondition()
            {
                return Task.FromResult(false);
            }

            //Act/Assert
            vm.SetCommandCondition(FalseCondition);
            await VerifyNavigationResultNullAsync(vm.Command);
        }
        
        [Fact]
        public async Task ExecutingCommand_AddedTrueCondition_ActionIsExecuted()
        {
            //Arrange
            var command = ReactiveCommand.Create(CommandAction);
            var vm = new HomeActionViewModel( MockContextProvider.Object, "Home");

            SetupViewModelForNavigationTest(vm);
            vm.SetCommand(command);

            Task<bool> TrueCondition()
            {
                return Task.FromResult(true);
            }

            //Act/Assert
            vm.SetCommandCondition(TrueCondition);
            await VerifyNavigationResultAsync<BogusViewModelTest>(vm.Command);
            _activated.Should().BeTrue();
        }

        
        private void SetupProjectId(Guid projectId)
        {
            MockGrandCentralStation.Setup(x => x.CurrentProjectId).Returns(projectId);
        }

        private IRoutableViewModel CommandAction()
        {
            _activated = true;
            return new BogusViewModelTest();
        }
        
        private class BogusViewModelTest: IRoutableViewModel
        {
            public string UrlPathSegment { get; } = string.Empty;

            public IScreen HostScreen { get; } = null;

#pragma warning disable 0067
			public event PropertyChangedEventHandler PropertyChanged;
            public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
#pragma warning restore 0067

			public void RaisePropertyChanging(System.ComponentModel.PropertyChangingEventArgs args)
            {
            }

            public void RaisePropertyChanged(PropertyChangedEventArgs args)
            {
            }
        }
    }
}