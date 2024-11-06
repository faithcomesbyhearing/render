using Moq;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Repositories.SectionRepository;
using Render.Repositories.SnapshotRepository;
using Render.Repositories.UserRepositories;
using Render.Services.SnapshotService;
using Render.Services.StageService;
using Render.Services.WorkflowService;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services.SnapshotServiceTests
{
    public class SnapshotServiceTests : TestBase
    {
        private Section _section;
        private User _draftUser1;
        private User _draftUser2;
        private User _user;
        private RenderWorkflow _workflow;
        private Stage _draftStage;
        private Stage _peerCheckStage;
        private Stage _commTestStage;
        private Stage _consCheckStage;
        private Stage _approvalStage;

        private readonly Mock<ISnapshotRepository> _mockSnapshotRepository;
        private readonly Mock<IAudioRepository<Audio>> _mockAudioRepository;
        private readonly Mock<IAudioRepository<Draft>> _mockDraftRepository;
        private readonly Mock<IAudioRepository<RetellBackTranslation>> _mockRetellRepository;
        private readonly Mock<IAudioRepository<SegmentBackTranslation>> _mockSegmentRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ISnapshotService _service;

        public SnapshotServiceTests()
        {
            _section = Section.UnitTestEmptySection();
            _user = new User("Consultant User", "consultant");
            _draftUser1 = new User("Draft User1", "draft1");
            _draftUser2 = new User("Draft User2", "draft2");
            _workflow = RenderWorkflow.Create(_section.ProjectId);
            _draftStage = _workflow.DraftingStage;
            _peerCheckStage = PeerCheckStage.Create();
            _commTestStage = CommunityTestStage.Create();
            _consCheckStage = ConsultantCheckStage.Create();
            _approvalStage = _workflow.ApprovalStage;

            _mockSnapshotRepository = new Mock<ISnapshotRepository>();
            var mockWorkflowService = new Mock<IWorkflowService>();
            var mockStageService = new Mock<IStageService>();
            var mockSectionRepository = new Mock<ISectionRepository>();
            _mockAudioRepository = new Mock<IAudioRepository<Audio>>();
            _mockDraftRepository = new Mock<IAudioRepository<Draft>>();
            _mockRetellRepository = new Mock<IAudioRepository<RetellBackTranslation>>();
            _mockSegmentRepository = new Mock<IAudioRepository<SegmentBackTranslation>>();
            _mockUserRepository = new Mock<IUserRepository>();

            _service = new SnapshotService(
                _mockSnapshotRepository.Object,
                mockWorkflowService.Object,
                mockStageService.Object,
                mockSectionRepository.Object,
                _mockAudioRepository.Object,
                _mockDraftRepository.Object,
                _mockRetellRepository.Object,
                _mockSegmentRepository.Object,
                _mockUserRepository.Object);
        }

        [Fact]
        public async Task ReturnBack_DefaultFlow_SaveAsyncInvoked3Times()
        {
            //Arrange
            _workflow.AddStage(_consCheckStage);
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var consCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: true);

            var consCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot, consCheckTempSnapshot, consCheckPermSnapshot, consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _consCheckStage, _consCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(3));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Exactly(2));
        }

        [Fact]
        public async Task ReturnBack_ConsCheckAdded_SaveAsyncInvokedTwice()
        {
            //Arrange
            _workflow.AddStage(_peerCheckStage);
            _workflow.AddStage(_consCheckStage);
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var peerCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: true);

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot, peerCheckTempSnapshot, peerCheckPermSnapshot, consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _consCheckStage, _consCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(2));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Once);
        }

        [Fact]
        public async Task ReturnBack_PeerCheckRemoved_ConsCheckAdded_SaveAsyncInvoked3Times()
        {
            //Arrange
            _workflow.AddStage(_consCheckStage);
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var peerCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: true);

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot, peerCheckTempSnapshot, peerCheckPermSnapshot, consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _consCheckStage, _consCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(3));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Once);
        }

        [Fact]
        public async Task ReturnBack_ConsCheckRemoved_SaveAsyncInvoked4Times()
        {
            //Arrange
            _workflow.AddStage(_peerCheckStage);
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var peerCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: true);

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false);

            var consCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: true);

            var consCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot,
                peerCheckTempSnapshot,
                peerCheckPermSnapshot,
                consCheckTempSnapshot,
                consCheckPermSnapshot,
                consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _peerCheckStage, _peerCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(4));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Exactly(2));
        }

        [Fact]
        public async Task ReturnBack_CommunityTestInserted_SaveAsyncInvoked3Times()
        {
            //Arrange
            _workflow.AddStage(_peerCheckStage);
            _workflow.AddStage(_commTestStage);
            _workflow.AddStage(_consCheckStage);
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var peerCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: true);

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false);

            var consCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: true);

            var consCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot,
                peerCheckTempSnapshot,
                peerCheckPermSnapshot,
                consCheckTempSnapshot,
                consCheckPermSnapshot,
                consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _consCheckStage, _consCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(3));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Exactly(2));
        }

        [Fact]
        public async Task ReturnBack_PeerCheckRemoved_ConsCheckRemoved_SaveAsyncInvoked5Times()
        {
            //Arrange
            var allStages = _workflow.GetAllStages().ToList();

            var snapshotList = new List<Snapshot>();

            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false);

            var peerCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: true);

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false);

            var consCheckTempSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: true);

            var consCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: false);

            var consApprTempSnapshot = GetFakeSnapshot(
                _section,
                _approvalStage,
                temporary: true);

            snapshotList.AddRange([draftPermSnapshot,
                peerCheckTempSnapshot,
                peerCheckPermSnapshot,
                consCheckTempSnapshot,
                consCheckPermSnapshot,
                consApprTempSnapshot]);

            _mockSnapshotRepository.Setup(x => x.GetSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);

            //Act
            await _service.ReturnBack(allStages, _section, _approvalStage, _peerCheckStage, _peerCheckStage.ReviseStep, _user.Id);

            //Assert
            _mockSnapshotRepository.Verify(r => r.SaveAsync(It.IsAny<Snapshot>()), Times.Exactly(5));
            _mockSnapshotRepository.Verify(r => r.BatchSoftDeleteAsync(It.IsAny<List<Snapshot>>(), _section), Times.Exactly(2));
        }

        [Fact]
        public async Task GetLastConflictedSnapshots_ConflictBetweenDraftAndConsCheck_Succeed()
        {
            var draftPermSnapshot = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false,
                createdBy: Guid.NewGuid());

            var peerCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _peerCheckStage,
                temporary: false,
                parentSnapshotId: draftPermSnapshot.Id);

            var consCheckPermSnapshot = GetFakeSnapshot(
                _section,
                _consCheckStage,
                temporary: false,
                parentSnapshotId : peerCheckPermSnapshot.Id);

            var draftPermSnapshot2 = GetFakeSnapshot(
                _section,
                _draftStage,
                temporary: false,
                createdBy: Guid.NewGuid());

            var snapshotList = new List<Snapshot>()
            {
                draftPermSnapshot, peerCheckPermSnapshot, consCheckPermSnapshot, draftPermSnapshot2,
            };

            var fakeRetells = new List<RetellBackTranslation>()
            {
                GetFakeRetell(consCheckPermSnapshot.Id)
            };
            var fakeRetell2 = GetFakeRetell(consCheckPermSnapshot.Id);
            var fakeSegments = new List<SegmentBackTranslation>()
            {
                GetFakeSegment(consCheckPermSnapshot.Id)
            };

            _mockSnapshotRepository.Setup(x => x.GetPermanentSnapshotsForSectionAsync(It.IsAny<Guid>()))
                .ReturnsAsync(snapshotList);
            _mockRetellRepository.Setup(x => x.GetMultipleByParentIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fakeRetells);
            _mockRetellRepository.Setup(x => x.GetByParentIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fakeRetell2);
            _mockSegmentRepository.Setup(x => x.GetMultipleByParentIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fakeSegments);

            _mockUserRepository.Setup(x => x.GetUserAsync(draftPermSnapshot.CreatedBy)).ReturnsAsync(_draftUser1);
            _mockUserRepository.Setup(x => x.GetUserAsync(draftPermSnapshot2.CreatedBy)).ReturnsAsync(_draftUser2);

            var actual = await _service.GetLastConflictedSnapshots(_section.Id);

            var expected = new List<ConflictedSnapshot>()
            {
                new ConflictedSnapshot
                {
                    Snapshot = draftPermSnapshot2,
                    TranslatorName = _draftUser2.FullName
                },
                new ConflictedSnapshot
                {
                    Snapshot = consCheckPermSnapshot,
                    TranslatorName = _draftUser1.FullName
                },
            };

            Assert.Equal(expected.Count(), actual.Count());

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Snapshot, actual[i].Snapshot);
                Assert.Equal(expected[i].TranslatorName, actual[i].TranslatorName);
            }
        }

        private static Snapshot GetFakeSnapshot(Section section, Stage stage, bool temporary, Guid createdBy = default, Guid parentSnapshotId = default)
        {
            return new Snapshot(
                sectionId: section.Id,
                checkedBy: default,
                approvedBy: default,
                approvedDate: section.ApprovedDate,
                createdBy: createdBy,
                scopeId: section.ScopeId,
                projectId: section.ProjectId,
                stageId: stage.Id,
                stepId: stage.Steps.FirstOrDefault().Id,
                passages: section.Passages,
                sectionReferenceAudioSnapshots: new List<SectionReferenceAudioSnapshot>(),
                noteInterpretationIds: new List<Guid>(),
                stageName: stage.Name,
                temporary: temporary,
                parentSnapshot: parentSnapshotId);
        }

        private static RetellBackTranslation GetFakeRetell(Guid correspondedSnapshotId)
        {
            return new RetellBackTranslation(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty)
            {
                CorrespondedSnashotId = correspondedSnapshotId
            };
        }

        private static SegmentBackTranslation GetFakeSegment(Guid correspondedSnapshotId)
        {
            var fakeTimeMarkers = new TimeMarkers(0, 10);
            return new SegmentBackTranslation(fakeTimeMarkers, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty)
            {
                CorrespondedSnashotId = correspondedSnapshotId
            };
        }
    }
}
