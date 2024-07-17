using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Components.TitleBar;
using Render.Components.TitleBar.MenuActions;
using Render.Interfaces;
using Render.Interfaces.AudioServices;
using Render.Interfaces.EssentialsWrappers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Kernel;
using Render.Repositories.LocalDataRepositories;
using Render.Services;
using Render.Services.SyncService;
using Render.TempFromVessel.Project;
using Splat;
using System.Reactive;
using System.Reactive.Linq;

namespace Render.UnitTests.App.Kernel
{
    public class ViewModelTestBase : TestBase
    {
        protected Mock<IViewModelContextProvider> MockContextProvider { get; }
        protected Mock<IGrandCentralStation> MockGrandCentralStation { get; }
        protected Mock<RenderWorkflow> MockRenderWorkflow { get; }

        protected ViewModelTestBase()
        {
            var mockScreen = new Mock<IScreen>();
            mockScreen.Setup(x => x.Router).Returns(new RoutingState());
            Locator.CurrentMutable.RegisterConstant(mockScreen.Object, typeof(IScreen));

            MockContextProvider = new Mock<IViewModelContextProvider>();
            MockGrandCentralStation = new Mock<IGrandCentralStation>();
            MockRenderWorkflow = new Mock<RenderWorkflow>(Guid.Empty, Guid.Empty);

            var mockLocalizationService = new Mock<ILocalizationService>();
            var mockAudioActivityService = new Mock<IAudioActivityService>();
            var mockMenuPopupService = new Mock<IMenuPopupService>();
            var mockSyncService = new Mock<ISyncService>();
            var mockLocalSyncService = new Mock<ILocalSyncService>();
            var mockTitleBarViewModel = new Mock<ITitleBarViewModel>();
            var mockRenderLogger = new Mock<IRenderLogger>();
            var mockMachineLoginStateRepository = new Mock<IMachineLoginStateRepository>();
            var mockEssentialsWrapper = new Mock<IEssentialsWrapper>();
            var mockTitleBarMenuViewModel = new Mock<TitleBarMenuViewModel>(
                new List<IMenuActionViewModel>(),
                MockContextProvider.Object,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Section>(),
                It.IsAny<PassageNumber>(),
                It.IsAny<Stage>(),
                It.IsAny<Step>(),
                It.IsAny<bool>());

            mockLocalizationService.Setup(x => x.GetCurrentLocalization()).Returns("en");
            MockContextProvider.Setup(x => x.GetLocalizationService()).Returns(mockLocalizationService.Object);

            MockContextProvider.Setup(x => x.GetMenuPopupService()).Returns(mockMenuPopupService.Object);
            MockContextProvider.Setup(x => x.GetSyncService()).Returns(mockSyncService.Object);
            MockContextProvider.Setup(x => x.GetLocalSyncService()).Returns(mockLocalSyncService.Object);

            mockTitleBarViewModel.Setup(x => x.NavigationItems).Returns(new DynamicDataWrapper<ReactiveCommand<Unit, IRoutableViewModel>>());
            mockTitleBarViewModel.Setup(x => x.TitleBarMenuViewModel).Returns(mockTitleBarMenuViewModel.Object);
            MockContextProvider.Setup(x => x.GetTitleBarViewModel(
                    It.IsAny<List<TitleBarElements>>(),
                    It.IsAny<TitleBarMenuViewModel>(),
                    It.IsAny<IViewModelContextProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<Audio>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(mockTitleBarViewModel.Object);
            MockContextProvider.Setup(x => x.GetAudioActivityService()).Returns(mockAudioActivityService.Object);
            MockContextProvider.Setup(x => x.GetLogger(It.IsAny<Type>())).Returns(mockRenderLogger.Object);
            MockContextProvider.Setup(x => x.GetGrandCentralStation()).Returns(MockGrandCentralStation.Object);

            mockMachineLoginStateRepository.Setup(x => x.GetMachineLoginState()).ReturnsAsync(new MachineLoginState());
            MockContextProvider.Setup(x => x.GetMachineLoginStateRepository()).Returns(mockMachineLoginStateRepository.Object);

            mockEssentialsWrapper.Setup(x => x.InvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(action => { action?.Invoke(); });
            MockContextProvider.Setup(x => x.GetEssentials()).Returns(mockEssentialsWrapper.Object);

            MockGrandCentralStation.Setup(x => x.ProjectWorkflow).Returns(MockRenderWorkflow.Object);

            var stage = new Stage();
            stage.SetName("Stage");
            MockRenderWorkflow.Setup(x => x.GetStage(It.IsAny<Guid>())).Returns(stage);
            
            MockContextProvider.Setup(x => x.GetModalService()).Returns(new Mock<IModalService>().Object);
        }

        protected void SetupMocksForHomeViewModelNavigation()
        {
            MockGrandCentralStation.Setup(x => x.StepsAssignedToUser())
                .Returns(new List<Guid>());
            MockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>());
            MockContextProvider.Setup(x => x.GetLoggedInUser())
                .Returns(new User("Navi", "navi"));

            //Home screen
            var projectRepositoryMock = new Mock<IDataPersistence<Project>>();
            var project = new Project("Project Name", string.Empty, string.Empty);
            projectRepositoryMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(project);
            MockContextProvider.Setup(x => x.GetPersistence<Project>()).Returns(projectRepositoryMock.Object);
        }

        protected void SetupViewModelForNavigationTest(ViewModelBase viewModel, IRoutableViewModel
             viewModelToAddToStack = null)
        {
            var mockScreen = new Mock<IScreen>();
            var mockRouter = new RoutingState();
            var mockMachineStateRepo = new Mock<IMachineLoginStateRepository>();

            if (viewModelToAddToStack != null)
            {
                mockRouter.Navigate.Execute(viewModelToAddToStack).Subscribe();
                mockRouter.Navigate.Execute(viewModel).Subscribe();
            }
            mockScreen.Setup(x => x.Router).Returns(mockRouter);
            viewModel.HostScreen = mockScreen.Object;
            MockContextProvider.Setup(mock => mock.GetSyncService()).Returns(new Mock<ISyncService>().Object);
            MockContextProvider.Setup(mock => mock.GetMachineLoginStateRepository())
                .Returns(mockMachineStateRepo.Object);
            mockMachineStateRepo.Setup(mock => mock.GetMachineLoginState())
                .ReturnsAsync(new MachineLoginState());
            MockGrandCentralStation.Setup(x => x.StepsAssignedToUser())
                .Returns(new List<Guid>());
            MockGrandCentralStation.Setup(x => x.SectionsAtStep(It.IsAny<Guid>()))
                .Returns(new List<Guid>());
            MockContextProvider.Setup(x => x.GetLoggedInUser()).Returns(new Mock<IUser>().Object);
        }

        protected static void VerifyNavigationResult<T>(ReactiveCommand<Unit, IRoutableViewModel> commandToTest)
        {
            var result = commandToTest.Execute().Wait();
            result.Should().BeOfType<T>();
        }

        protected static async Task VerifyNavigationResultAsync<T>(ReactiveCommand<Unit, IRoutableViewModel> commandToTest)
        {
            commandToTest.ThrownExceptions.Subscribe(err => Console.WriteLine(err.ToString()));
            var result = await commandToTest.Execute();
            result.Should().BeOfType<T>();
        }

        protected static void VerifyNavigationResultNull(ReactiveCommand<Unit, IRoutableViewModel> commandToTest)
        {
            IRoutableViewModel result = null;
            commandToTest.Execute().Subscribe(item => result = item);
            result.Should().BeNull();
        }

        protected static async Task VerifyNavigationResultNullAsync(ReactiveCommand<Unit, IRoutableViewModel> commandToTest)
        {
            var result = await commandToTest.Execute();
            result.Should().BeNull();
        }

        protected IRenderLogger GetLogger()
        {
            return MockContextProvider.Object.GetLogger(GetType());
        }
    }
}