using Render.Models.Sections.CommunityCheck;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Sections
{
    public class QuestionTests : TestBase
    {
        [Fact]
        public void Constructor_NewStageId_AddedToCollection()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var question = new Question(stageId);
           
            //Act & Assert
            question.StageIds.Should().NotBeEmpty();
            question.StageIds.Should().Contain(stageId);
        }
        
        [Fact]
        public void Constructor_StageIdAsEmptyGuid_IsNotAddedToCollection()
        {
            //Arrange
            var question = new Question(default);
           
            //Act & Assert
            question.StageIds.Should().NotContain(Guid.Empty);
            question.StageIds.Should().BeEmpty();
        }
       
        [Fact]
        public void AddStage_NewStageId_AddedToCollection()
        {
            //Arrange
            var question = new Question(default);
           
            //Act
            var stageId = Guid.NewGuid();
            question.AddStage(stageId);
            
            //Assert
            question.StageIds.Should().Contain(stageId);
            question.StageIds.Should().HaveCount(1);
        }
        
        [Fact]
        public void AddStage_StageIdAsEmptyGuid_ThrowsArgumentException()
        {
            //Arrange
            var question = new Question(default);
           
            //Act & Assert
            Assert.Throws<ArgumentException>(() => question.AddStage(Guid.Empty));
        }
        
        [Fact]
        public void AddStage_CallTwiceWithTheSameStageId_CollectionDoesNotContainDuplicates()
        {
            //Arrange
            var question = new Question(default);
           
            //Act
            var stageId = Guid.NewGuid();
            question.AddStage(stageId);
            question.AddStage(stageId);
            
            //Assert
            question.StageIds.Should().HaveCount(1);
        }
        
        [Fact]
        public void RemoveFromStage_ExistingStageId_RemovedFromCollection()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var question = new Question(stageId);
           
            //Act
            question.RemoveFromStage(stageId);
            
            //Assert
            question.StageIds.Should().HaveCount(0);
        }
        
        [Fact]
        public void RemoveFromStage_NonExistingStageId_CollectionDoesNotChanged()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var question = new Question(stageId);
           
            //Act
            question.RemoveFromStage(Guid.NewGuid());
            
            //Assert
            question.StageIds.Should().HaveCount(1);
        }
        
        [Fact]
        public void RemoveFromStage_MultipleStageIds_RemovedFromCollection()
        {
            //Arrange
            var stageIds = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToList();
            var question = new Question(default);
            question.AddStage(stageIds.ElementAt(0));
            question.AddStage(stageIds.ElementAt(1));
           
            //Act
            question.RemoveFromStage(stageIds);
            
            //Assert
            question.StageIds.Should().HaveCount(0);
        }
    }
}