using System.Reactive;
using FluentAssertions;
using Moq;
using ReactiveUI;
using Render.Components.AudioRecorder;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.PassageListen;
using Render.Pages.Translator.SectionListen;
using Render.Repositories.SectionRepository;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.SessionStateServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Translator
{
    public class SectionListenViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Step _step;
        private readonly Stage _stage;

        public SectionListenViewModelTests()
        {
            _section = Section.UnitTestEmptySection();
            _step = new Step(RenderStepTypes.Draft, Roles.Drafting);
            _stage = new Stage();
            _stage.SetName("Stage");     
            var supplementaryAudio = new Audio(Guid.Empty, _section.ProjectId, _section.Id);
            var supplementaryMaterial = new SupplementaryMaterial("Supplementary", supplementaryAudio.Id);
            _section.SetSupplementaryMaterial(new List<SupplementaryMaterial>() { supplementaryMaterial });

            var mockSequencerFactory = new Mock<ISequencerFactory>();
            mockSequencerFactory.Setup(x => x.CreateRecorder(
                It.IsAny<Func<IAudioPlayer>>(),
                It.IsAny<Func<IAudioRecorder>>(),
                It.IsAny<FlagType>()
            )).Returns(new Mock<ISequencerRecorderViewModel>().Object);
            MockContextProvider.Setup(x => x.GetSequencerFactory()).Returns(mockSequencerFactory.Object);
            
            MockContextProvider.Setup(x => x.GetBarPlayerViewModel(
                    It.IsAny<Audio>(),
                    It.IsAny<ActionState>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    null,
                    null,
                    It.IsAny<ImageSource>(),
                    It.IsAny<ReactiveCommand<Unit, IRoutableViewModel>>(),
                    It.IsAny<IObservable<bool>>(),
                    It.IsAny<string>()))
                .Returns(new Mock<IBarPlayerViewModel>().Object);
            
            var mockSessionService = new Mock<ISessionStateService>();
            mockSessionService.Setup(x => x.AudioIds).Returns(new List<Guid>());
            mockSessionService.Setup(x => x.ActiveSession)
                .Returns(new UserProjectSession(Guid.Empty, Guid.Empty, Guid.Empty));
            MockContextProvider.Setup(x => x.GetSessionStateService()).Returns(mockSessionService.Object);
        }
        
        [Fact]
        public async Task ViewModelCreation_Succeeds()
        {
            //Arrange
            var contentProvider = MockContextProvider.Object;

            //Act
            var vm = await SectionListenViewModel.CreateAsync(contentProvider, _section, _step, _stage);

            //Assert
            vm.UrlPathSegment.Should().NotBeNull();
            vm.ActionViewModelBaseSourceList.Count.Should().Be(1);
        }

        [Fact]
        public async Task NavigateForward_WhenPassageListenOn_Succeeds()
        {
            //Arrange
            var mockSectionRepository = new Mock<ISectionRepository>();
            MockContextProvider.Setup(x => x.GetSectionRepository())
                .Returns(mockSectionRepository.Object);
            _step.StepSettings.SetSetting(SettingType.DoPassageListen, true);
            var vm = await SectionListenViewModel.CreateAsync(MockContextProvider.Object, _section, _step, _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            await VerifyNavigationResultAsync<PassageListenViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
        
        [Fact]
        public async Task NavigateForward_WhenPassageListenOff_Succeeds()
        {
            //Arrange
            _step.StepSettings.SetSetting(SettingType.DoPassageListen, false);

            MockGrandCentralStation.Setup(x => x.ProjectWorkflow).Returns(RenderWorkflow.Create(Guid.Empty));
            var vm = await SectionListenViewModel.CreateAsync(MockContextProvider.Object, _section, _step, _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            await VerifyNavigationResultAsync<DraftingViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
    }
}