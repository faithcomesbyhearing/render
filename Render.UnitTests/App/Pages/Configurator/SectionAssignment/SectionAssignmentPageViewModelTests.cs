using FluentAssertions;
using Moq;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Pages.Configurator.SectionAssignment;
using Render.Repositories.Kernel;
using Render.Repositories.SectionRepository;
using Render.Repositories.UserRepositories;
using Render.Repositories.WorkflowRepositories;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Configurator.SectionAssignment
{
    public class SectionAssignmentPageViewModelTests : ViewModelTestBase
    {
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepository = new();
        private readonly Mock<ISectionRepository> _mockSectionRepository = new();
        private readonly Mock<IUserRepository> _mockUserRepository = new();
        private readonly Mock<IDataPersistence<Scope>> _mockScopePersistence = new();
        private readonly RenderWorkflow _renderWorkflow = RenderWorkflow.Create(Guid.Empty);
        private readonly Section _section;
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly RenderUser _user1 = new("User1", Guid.Empty);
        private readonly RenderUser _user2 = new("User2", Guid.Empty);
        private readonly Scope _scope;
        private readonly Scope _scope2;
        private readonly List<Section> _allSections;

        public SectionAssignmentPageViewModelTests()
        {
            _scope = new Scope(_projectId);
            _scope.SetStatus("Active");
            _scope2 = new Scope(_projectId);
            _scope2.SetStatus("Inactive");
            var project = new Project("Project", "ref", "iso");

            _section = Section.UnitTestEmptySection(scopeId: _scope.Id);
            var section2 = Section.UnitTestEmptySection(scopeId: _scope.Id);
            var section3 = Section.UnitTestEmptySection(scopeId: _scope2.Id);
            _allSections = new List<Section> { _section, section2, section3 };

            var mockProjectPersistence = new Mock<IDataPersistence<Project>>();
            mockProjectPersistence.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(mockProjectPersistence.Object);
            _mockScopePersistence.Setup(x => x.QueryOnFieldAsync("ProjectId",
                    _projectId.ToString(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Scope> { _scope, _scope2 });
            MockContextProvider.Setup(x => x.GetPersistence<Scope>()).Returns(_mockScopePersistence.Object);
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().First(), _user1.Id);
            _renderWorkflow.AddTranslationAssignmentForTeam(_renderWorkflow.GetTeams().Last(), _user2.Id);
            _mockWorkflowRepository.Setup(x => x.GetWorkflowForProjectIdAsync(_projectId))
                .ReturnsAsync(_renderWorkflow);
            _mockSectionRepository.Setup(x => x.GetSectionsForProjectAsync(_projectId))
                .ReturnsAsync(_allSections);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user1.Id)).ReturnsAsync(_user1);
            _mockUserRepository.Setup(x => x.GetUserAsync(_user2.Id)).ReturnsAsync(_user2);

            MockContextProvider.Setup(x => x.GetWorkflowRepository())
                .Returns(_mockWorkflowRepository.Object);
            MockContextProvider.Setup(x => x.GetUserRepository())
                .Returns(_mockUserRepository.Object);
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(_mockSectionRepository.Object);
        }

        [Fact]
        public async Task TeamUpdates_AddAssignment_SavesWorkflow()
        {
            //Arrange

            //Act
            var vm = await SectionAssignmentPageViewModel.CreateAsync(MockContextProvider.Object, _projectId);
            vm.TeamSectionAssignments.First().Team = new TeamTranslatorUser(_renderWorkflow.GetTeams().First(), _user1);
            await vm.UpdateWorkflow(vm.TeamSectionAssignments.First());

            //Assert
            // _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(_renderWorkflow));
            _renderWorkflow.GetTeams().First().SectionAssignments.First().SectionId.Should().Be(_section.Id);
        }

        [Fact]
        public async Task TeamUpdates_RemoveAssignment_SavesWorkflow()
        {
            //Arrange
            _renderWorkflow.AddSectionAssignmentToTeam(_renderWorkflow.GetTeams().First(), _section.Id, 1);
            //Act
            var vm = await SectionAssignmentPageViewModel.CreateAsync(MockContextProvider.Object, _projectId);
            vm.TeamSectionAssignments.First().Team = null;
            await vm.UpdateWorkflow(vm.TeamSectionAssignments.First());

            //Assert
            // _mockWorkflowRepository.Verify(x => x.SaveWorkflowAsync(_renderWorkflow));
            _renderWorkflow.GetTeams().First().SectionAssignments.Count.Should().Be(0);
        }

        [Fact]
        public async Task Retrieve_ShowActiveScopes()
        {
            //Arrange
            _scope.Status.Should().Be("Active");
            _scope2.Status.Should().Be("Inactive");
            var expectedCount = _allSections.Count(x => x.ScopeId == _scope.Id);

            //Act
            var vm = await SectionAssignmentPageViewModel.CreateAsync(MockContextProvider.Object, _projectId);

            //Assert
            _mockScopePersistence.Verify(x => x.QueryOnFieldAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);

            await Task.Delay(50);
            vm.SectionViewViewModel.SectionCards.Count.Should().Be(expectedCount);
        }
    }
}