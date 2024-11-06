using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using FluentAssertions.Execution;
using Render.Components.TitleBar;
using Render.Components.TitleBar.MenuActions;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Components.TitleBar
{
    public class TitleBarMenuViewModelTests : ViewModelTestBase
    {
        private const string _projectId = "11111111-1111-1111-1111-111111111111";
        private const string _projectIdEmpty = "00000000-0000-0000-0000-000000000000";

        public TitleBarMenuViewModelTests()
        {
            var user = new User("Test User", "TestUser");
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(user);
        }

        private TitleBarMenuViewModel GetViewModel(string urlPath, string pageName, Step step = null,
            PassageNumber passageNumber = null)
        {
            return new TitleBarMenuViewModel(
                new List<IMenuActionViewModel>(),
                MockContextProvider.Object,
                urlPath,
                pageName,
                step: step,
                passageNumber: passageNumber);
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, false)]
        public void Construction_NotShowProject_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel("ProjectSelect", "Home");

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowProjectList.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_NotShowProjectHome_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel("Home", "Home");

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().BeFalse();
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_NotShowSectionStatus_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel("SectionStatusPage", "Home");

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_NotShowUserSettings_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel("UserSettings", "Home");

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_NotShowAudioExport_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel("AudioExportPage", "Home");

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_ShowDividePassageOnDrafting_Succeeds(string projectId, bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var draftingStep = new Step(RenderStepTypes.Draft, Roles.Drafting,
                settings: WorkflowSettings.StandardReviseSettings);
            var vm = GetViewModel(
                "Drafting",
                "Draft",
                step: draftingStep,
                passageNumber: new PassageNumber(1));

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeTrue();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(_projectIdEmpty, false)]
        [InlineData(_projectId, true)]
        public void Construction_NotShowDividePassageOnDrafting_WithDividedPassage_Succeeds(string projectId,
            bool expected)
        {
            //Arrange
            MockGrandCentralStation.SetupGet(x => x.CurrentProjectId).Returns(new Guid(projectId));
            var vm = GetViewModel(
                "Drafting",
                "Draft",
                passageNumber: new PassageNumber(1, 1));

            //Assert
            using (new AssertionScope())
            {
                vm.Actions.Items.ToList().Should().BeEmpty();
                vm.ShowUser.Should().BeTrue();
                vm.ShowActionItems.Should().BeFalse();
                vm.ShowDividePassage.Should().BeFalse();
                vm.ShowProjectList.Should().BeTrue();
                vm.ShowProjectHome.Should().Be(expected);
                vm.ShowSectionStatus.Should().Be(expected);
            }
        }
    }
}