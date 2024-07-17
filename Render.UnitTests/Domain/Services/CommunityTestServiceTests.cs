using Moq;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.SectionRepository;
using Render.Services;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Services
{
    public class CommunityTestServiceTests : TestBase
    {
        private readonly Mock<ICommunityTestRepository> _mockCommunityTestRepository;
        private readonly Mock<ISectionRepository> _mockSectionRepository;
        private readonly CommunityTestService _service;

        private readonly Guid _stage1 = Guid.NewGuid();
        private readonly Guid _stage2 = Guid.NewGuid();

        private readonly Section _section = new Section(
            new SectionTitle("", default),
            default, default, default, default, default,
            passages: new List<Passage>
            {
                new Passage(new PassageNumber(default)) { CurrentDraftAudio = new Draft(default, default, default) }
            });

        public CommunityTestServiceTests()
        {
            _mockCommunityTestRepository = new Mock<ICommunityTestRepository>();
            _mockSectionRepository = new Mock<ISectionRepository>();

            _mockSectionRepository.Setup(x => x.GetSectionWithDraftsAsync(_section.Id, false, false, true, false))
                .ReturnsAsync(_section);

            _service = new CommunityTestService(_mockCommunityTestRepository.Object, _mockSectionRepository.Object);
        }

        // GetCommunityTestForStage tests
        [Fact]
        public void GetCommunityTestForStage_StagesIsNull_ThrowsArgumentNullException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetCommunityTestForStage(null, new CommunityTest(default, default, default), new List<Stage>()));
        }

        [Fact]
        public void GetCommunityTestForStage_CommunityTestIsNull_ThrowsArgumentNullException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetCommunityTestForStage(new Stage(), null, new List<Stage>()));
        }

        [Fact]
        public void GetCommunityTestForStage_WorkflowStagesIsNull_ThrowsArgumentNullException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetCommunityTestForStage(new Stage(), new CommunityTest(default, default, default), null));
        }

        [Fact]
        public void GetCommunityTestForStage_WorkflowStagesIsEmpty_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _service.GetCommunityTestForStage(new Stage(), new CommunityTest(default, default, default), new List<Stage>()));
        }

        [Fact]
        public void GetCommunityTestForStage_Stage_StageIdIsSet()
        {
            //Arrange
            var workflowStages = new List<Stage>
            {
                new(StageTypes.CommunityTest)
            };

            var currentStage = workflowStages.First();
            var communityTest = new CommunityTest(default, default, default);

            //Act
            var communityTestForStage = _service.GetCommunityTestForStage(currentStage, communityTest, workflowStages);

            //Assert
            communityTestForStage.StageId.Should().Be(currentStage.Id);
        }

        [Fact]
        public void GetCommunityTestForStage_TwoConsecutiveStages_RetellsAreFromBothStages()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.CommunityTest);
            var stage2 = new Stage(StageTypes.CommunityTest);

            var workflowStages = new List<Stage>
            {
                stage1,
                stage2
            };

            var communityTest = new CommunityTest(default, default, default);
            communityTest.AddRetell(new CommunityRetell(stage1.Id, default, default, default));
            communityTest.AddRetell(new CommunityRetell(stage2.Id, default, default, default));

            //Act
            var communityTestForStage = _service.GetCommunityTestForStage(stage2, communityTest, workflowStages);

            //Assert
            communityTestForStage.Retells.Count.Should().Be(2);
        }

        [Fact]
        public void GetCommunityTestForStage_StagesNotConsecutive_RetellsAreOnlyForTheCurrentStage()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.CommunityTest);
            var stage2 = new Stage(StageTypes.CommunityTest);

            var workflowStages = new List<Stage>
            {
                stage1,
                new Stage(),
                stage2
            };

            var communityTest = new CommunityTest(default, default, default);
            communityTest.AddRetell(new CommunityRetell(stage1.Id, default, default, default));
            communityTest.AddRetell(new CommunityRetell(stage2.Id, default, default, default));

            //Act
            var communityTestForStage = _service.GetCommunityTestForStage(stage2, communityTest, workflowStages);

            //Assert
            communityTestForStage.Retells.Count.Should().Be(1);
            communityTestForStage.Retells.Single().StageId.Should().Be(stage2.Id);
        }

        [Fact]
        public void GetCommunityTestForStage_Flags_Initialized()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.CommunityTest);

            var workflowStages = new List<Stage>
            {
                stage1,
            };

            var communityTest = new CommunityTest(default, default, default);
            communityTest.AddFlag(new Flag(default) { Questions = new List<Question> { new(stage1.Id) } });

            //Act
            var communityTestForStage = _service.GetCommunityTestForStage(stage1, communityTest, workflowStages);

            //Assert
            communityTestForStage.Flags.Count.Should().Be(1);
        }

        // CopyFlagsAsync tests
        [Fact]
        public async void CopyFlagsAsync_DraftWithCommunityTest_SaveCommunityTestAsyncIsInvoked()
        {
            //Arrange
            var communityTest = _section.Passages.Single().CurrentDraftAudio.GetCommunityCheck();

            //Act
            await _service.CopyFlagsAsync(_stage1, _stage2, _section.Id);

            //Assert
            _mockCommunityTestRepository.Verify(x => x.SaveCommunityTestAsync(communityTest), Times.Once);
        }

        [Fact]
        public async void CopyFlagsAsync_DraftWithoutCommunityTest_SaveCommunityTestAsyncIsNotInvoked()
        {
            //Arrange
            //Act
            await _service.CopyFlagsAsync(_stage1, _stage2, _section.Id);

            //Assert
            _mockCommunityTestRepository.Verify(x => x.SaveCommunityTestAsync(It.IsAny<CommunityTest>()), Times.Never);
        }

        [Fact]
        public async void CopyFlagsAsync_EmptySection_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CopyFlagsAsync(_stage1, _stage2, default));
        }

        [Fact]
        public async void CopyFlagsAsync_EmptyFromStageOrEmptyToStage_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CopyFlagsAsync(default, _stage2, _section.Id));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CopyFlagsAsync(_stage1, default, _section.Id));
        }

        [Fact]
        public async void CopyFlagsAsync_EmptySection_GetSectionWithDraftsAsyncIsNotInvoked()
        {
            //Arrange
            //Act
            try
            {
                await _service.CopyFlagsAsync(_stage1, _stage2, default);
            }
            catch
            {
                // ignored
            }

            //Assert
            _mockSectionRepository.Verify(x => x.GetSectionWithDraftsAsync(_section.Id, false, false, true, false), Times.Never);
        }

        // GetPreviousConsecutiveStages tests

        [Fact]
        public void GetPreviousConsecutiveStages_WorkflowStagesIsNull_ThrowsArgumentNullException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetPreviousConsecutiveStages(null, Guid.NewGuid()));
        }

        [Fact]
        public void GetPreviousConsecutiveStages_WorkflowStagesIsEmpty_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _service.GetPreviousConsecutiveStages(new List<Stage>(), Guid.NewGuid()));
        }

        [Fact]
        public void GetPreviousConsecutiveStages_StageIdIsEmpty_ThrowsArgumentException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _service.GetPreviousConsecutiveStages(new List<Stage> { new Stage(StageTypes.CommunityTest) }, default));
        }

        [Fact]
        public void GetPreviousConsecutiveStages_StageDoesNotExistInWorkflowStages_ArgumentException()
        {
            //Arrange
            //Act
            //Assert
            Assert.Throws<Exception>(() => _service.GetPreviousConsecutiveStages(new List<Stage> { new Stage(StageTypes.CommunityTest) }, Guid.NewGuid()));
        }

        [Fact]
        public void GetPreviousConsecutiveStages_SingleStage_ReturnsEmptyList()
        {
            //Arrange
            var stage = new Stage(StageTypes.CommunityTest);
            var workflowStages = new List<Stage> { stage };

            //Act
            var stages = _service.GetPreviousConsecutiveStages(workflowStages, stage.Id);

            //Assert
            stages.Should().NotBeNull();
            stages.Should().BeEmpty();
        }

        [Fact]
        public void GetPreviousConsecutiveStages_StageOnlyAfterTheCurrentStage_ReturnsEmptyList()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.CommunityTest);
            var stage2 = new Stage(StageTypes.CommunityTest);
            var workflowStages = new List<Stage> { stage1, stage2 };

            //Act
            var stages = _service.GetPreviousConsecutiveStages(workflowStages, stage1.Id);

            //Assert
            stages.Should().NotBeNull();
            stages.Should().BeEmpty();
        }

        [Fact]
        public void GetPreviousConsecutiveStages_PreviousStageIsCommunityTest_ReturnsPreviousStage()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.CommunityTest);
            var stage2 = new Stage(StageTypes.CommunityTest);
            var workflowStages = new List<Stage> { stage1, stage2 };

            //Act
            var stages = _service.GetPreviousConsecutiveStages(workflowStages, stage2.Id);

            //Assert
            stages.Should().NotBeEmpty();
            stages.Single().Should().Be(stage1.Id);
        }

        [Fact]
        public void GetPreviousConsecutiveStages_PreviousStageIsDrafting_ReturnsEmptyList()
        {
            //Arrange
            var stage1 = new Stage(StageTypes.Drafting);
            var stage2 = new Stage(StageTypes.CommunityTest);
            var workflowStages = new List<Stage> { stage1, stage2 };

            //Act
            var stages = _service.GetPreviousConsecutiveStages(workflowStages, stage2.Id);

            //Assert
            stages.Should().BeEmpty();
        }

        [Fact]
        public void GetPreviousConsecutiveStages_PeerCheckInBetween_ReturnsEmptyList()
        {
            //Arrange
            var workflowStages = new List<Stage>
            {
                new(StageTypes.CommunityTest),
                new(StageTypes.PeerCheck),
                new(StageTypes.CommunityTest)
            };

            //Act
            var stages = _service.GetPreviousConsecutiveStages(workflowStages, workflowStages.Last().Id);

            //Assert
            stages.Should().BeEmpty();
        }
    }
}