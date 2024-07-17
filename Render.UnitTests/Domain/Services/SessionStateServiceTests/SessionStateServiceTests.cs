using Moq;
using Render.Models.Audio;
using Render.Models.LocalOnlyData;
using Render.Models.Sections;
using Render.Repositories.Audio;
using Render.Repositories.LocalDataRepositories;
using Render.Services.SessionStateServices;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.SessionStateServiceTests
{
    public class SessionStateServiceTests : TestBase
    {
        private readonly Mock<ISessionStateRepository> _mockSessionRepo;
        private readonly Mock<IAudioRepository<Audio>> _mockAudioRepo;
        private readonly Guid _userId;
        private readonly Guid _projectId;
        private readonly Guid _stepId;
        private readonly UserProjectSession _session;

        public SessionStateServiceTests()
        {
            _mockSessionRepo = new Mock<ISessionStateRepository>();
            _mockAudioRepo = new Mock<IAudioRepository<Audio>>();

            _userId = Guid.NewGuid();
            _projectId = Guid.NewGuid();
            _stepId = Guid.NewGuid();
            _session = new UserProjectSession(_userId, _projectId, _stepId);

            _mockSessionRepo.Setup(x =>
                    x.GetUserProjectSessionAsync(_userId, _projectId))
                .ReturnsAsync(new List<UserProjectSession> { _session });
        }

        [Fact]
        public async Task SetActiveSession_Succeeds()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            //Act
            await SetupService(service);

            //Assert
            service.ActiveSession.Should().Be(_session);
            _mockSessionRepo.Verify(x => x.GetUserProjectSessionAsync(_userId, _projectId));
        }

        [Fact]
        public async Task AddDraftAudio_Succeeds()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            //Act
            service.AddDraftAudio(audio);
            //Assert
            service.AudioIds.Should().Contain(audio.Id);
        }

        [Fact]
        public async Task AddRequirementCompletion_Succeeds()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var id = Guid.NewGuid();
            //Act
            service.AddRequirementCompletion(id);
            //Assert
            service.RequirementMetInSession(id).Should().BeTrue();
        }

        [Fact]
        public async Task RequirementMetInSession_WhenNotMet_ReturnsFalse()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);

            //Act
            var result = service.RequirementMetInSession(Guid.NewGuid());

            //Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAudio_Succeeds()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var audio = new Audio(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            //Act
            service.AddDraftAudio(audio);
            service.RemoveDraftAudio(audio.Id);

            //Assert
            service.AudioIds.Should().NotContain(audio.Id);
        }

        [Fact]
        public async Task RemoveDraftAudios_Audio_RemovedSuccessfully()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);

            //Act
            service.AddDraftAudio(new Audio(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
            await service.RemoveDraftAudios(service.AudioIds);
            
            //Assert
            service.AudioIds.Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveDraftAudios_DeletesAudiosProperly()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var existingAudio = new Audio(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            service.AddDraftAudio(existingAudio);

            var audios = Enumerable.Range(0, 2)
                .Select(_ => new Audio(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
                .ToList();

            //Act
            foreach (var audio in audios)
            {
                service.AddDraftAudio(audio);
            }

            await service.RemoveDraftAudios(audios.Select(i => i.Id));

            //Assert
            service.AudioIds.Should().Contain(existingAudio.Id);
            service.AudioIds.Should().NotContain(audios.Select(i => i.Id));
        }

        [Fact]
        public async Task StartPage_NewPageAndSection_DeletesOldAudioAndSetsValuesProperly()
        {
            //Arrange
            var session = new UserProjectSession(_userId, _projectId, _stepId);
            var audioId = Guid.NewGuid();
            session.DraftAudioIds.Add(audioId);

            _mockSessionRepo.Setup(x => x.GetUserProjectSessionAsync(_userId, _projectId))
                .ReturnsAsync(new List<UserProjectSession> { session });
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var sectionId = Guid.NewGuid();

            //Act
            service.StartPage(sectionId, "New Page", new PassageNumber(1), Guid.Empty);
            //our truly async delete messes with us, need to wait just a tiny bit for this for when we run all tests
            //Assert
            service.ActiveSession.PageName.Should().Be("New Page");
            service.AudioIds.Count.Should().Be(0);
            service.ActiveSession.SectionId.Should().Be(sectionId);
        }

        [Fact]
        public async Task StartPage_WhenPageIsNotNew_ChangesNothing()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var sectionId = Guid.NewGuid();
            _session.StartStep(sectionId, "Page");
            _session.SetPassageNumber(new PassageNumber(1));
            _session.DraftAudioIds.Add(Guid.NewGuid());

            //Act
            service.StartPage(sectionId, "Page", new PassageNumber(1), Guid.Empty);

            //Assert
            service.ActiveSession.PageName.Should().Be("Page");
            service.ActiveSession.SectionId.Should().Be(sectionId);
            service.AudioIds.Count.Should().Be(1);
            _mockAudioRepo.Verify(x => x.DeleteAudioByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task StartPage_WhenJustPageNameChanges_ChangesPageNameAndSaves()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            var sectionId = Guid.NewGuid();
            _session.StartStep(sectionId, "Page");
            _session.SetPassageNumber(new PassageNumber(1));
            _session.DraftAudioIds.Add(Guid.NewGuid());

            //Act
            service.StartPage(sectionId, "New Page", new PassageNumber(1), Guid.Empty);

            //Assert
            service.ActiveSession.PageName.Should().Be("New Page");
            service.ActiveSession.SectionId.Should().Be(sectionId);
            service.AudioIds.Count.Should().Be(1);
            _mockAudioRepo.Verify(x => x.DeleteAudioByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task RequirementMetInSession_DoesNotModify_RequirementsCompletedCollection()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);

            //Act
            service.RequirementMetInSession(Guid.NewGuid());

            //Assert
            service.ActiveSession.RequirementsCompleted.Count.Should().Be(0);
        }

        [Fact]
        public async Task StartPage_RequirementsCompleted_AreNotCleared()
        {
            //Arrange
            var service = new SessionStateService(_mockSessionRepo.Object, _mockAudioRepo.Object);
            await SetupService(service);
            _session.AddRequirementCompletion(Guid.NewGuid());

            //Act
            service.StartPage(Guid.NewGuid(), "", new PassageNumber(1), Guid.Empty);

            //Assert
            service.ActiveSession.RequirementsCompleted.Count.Should().Be(1);
        }

        private async Task SetupService(SessionStateService service)
        {
            var sectionId = Guid.NewGuid();
            await service.LoadUserProjectSessionAsync(_userId, _projectId);
            service.SetCurrentStep(_stepId, sectionId);
            service.GetSessionPage(_stepId, sectionId, new PassageNumber(1));
        }
    }
}