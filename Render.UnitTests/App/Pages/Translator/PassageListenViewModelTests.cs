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
using Render.Repositories.SectionRepository;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using Render.Services.SessionStateServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.Translator
{
    public class PassageListenViewModelTests : ViewModelTestBase
    {
        private readonly Section _section;
        private readonly Step _step;
        private readonly Stage _stage;

        public PassageListenViewModelTests()
        {
            _section = Section.UnitTestEmptySection();
            _step = new Step(RenderStepTypes.Draft, Roles.Drafting);
            _stage = new Stage();
            _stage.SetName("Stage");
            
            var supplementaryAudio = new Audio(Guid.Empty, _section.ProjectId, _section.Id);
            var supplementaryMaterial = new SupplementaryMaterial("Supplementary", supplementaryAudio.Id);
            _section.SetSupplementaryMaterial(new List<SupplementaryMaterial>() { supplementaryMaterial });

            var mockSectionRepository = new Mock<ISectionRepository>();
            mockSectionRepository.Setup(x => x.GetSectionAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(_section);
            MockContextProvider.Setup(x => x.GetSectionRepository()).Returns(mockSectionRepository.Object);
            
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
            var vm = await PassageListenViewModel.CreateAsync(contentProvider, _section, _step, _section.Passages
            .First(), _stage);

            //Assert
            vm.UrlPathSegment.Should().NotBeNull();
            vm.ActionViewModelBaseSourceList.Count.Should().Be(1);
        }
        
        [Fact]
        public async Task NavigateForward_Succeeds()
        {
            //Arrange
            var vm = await PassageListenViewModel.CreateAsync(MockContextProvider.Object, _section, _step, _section
            .Passages.First(), _stage);
            SetupViewModelForNavigationTest(vm);
            
            //Act & Assert
            await VerifyNavigationResultAsync<DraftingViewModel>(vm.ProceedButtonViewModel.NavigateToPageCommand);
        }
    }
}