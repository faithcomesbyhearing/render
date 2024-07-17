using Render.Models.Sections.CommunityCheck;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Sections
{
    public class CommunityTestTests : TestBase
    {
        private readonly CommunityTest _communityTest = new CommunityTest(default, default, default);
        
        [Fact]
        public void AddFlag_AddsEmptyFlag()
        {
            //Arrange
            
            //Act
            var flag = _communityTest.AddFlag(0);

            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(1);
            flag.TimeMarker.Should().Be(0);
        }

        [Fact]
        public void AddQuestionToFlag_AddsEmptyQuestion()
        {
            //Arrange
            
            //Act
            var flag = _communityTest.AddFlag(0);
            var stageId = Guid.NewGuid();
            var question = flag.AddQuestion(stageId, Guid.Empty, Guid.Empty);

            //Assert
            question.StageIds.Should().Contain(stageId);
            flag.Questions.Count.Should().Be(1);
        }

        [Fact]
        public void AddQuestion_WhenAQuestionDoesNotHaveAudio_Fails()
        {
            //Arrange

            //Act
            var flag = _communityTest.AddFlag(0);
            var stageId = Guid.NewGuid();
            var firstQuestion = flag.AddQuestion(stageId, Guid.Empty, Guid.Empty);
            var result = flag.AddQuestion(stageId, Guid.Empty, Guid.Empty);

            //Assert
            firstQuestion.Should().NotBeNull();
            result.Should().BeNull();
        }

        [Fact]
        public void CopyFlags_StageWithoutFlags_NothingWasCopied()
        {
            //Arrange
            var stage1 = Guid.NewGuid();
            var stage2 = Guid.NewGuid();
            
            //Act
            _communityTest.CopyFlags(stage1, stage2);
            
            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(0);
        }
        
        [Fact]
        public void CopyFlags_StageWithFlag_FlagAreCopiedToNextStage()
        {
            //Arrange
            var stage1 = Guid.NewGuid();
            var stage2 = Guid.NewGuid();

            var f = _communityTest.AddFlag(0);
            f.AddQuestion(stage1, default, default);
            
            //Act
            _communityTest.CopyFlags(stage1, stage2);
            
            //Assert
            var question = _communityTest.GetFlags(stage2).Single().Questions.Single();
            question.StageIds.Contains(stage1).Should().BeTrue();
            question.StageIds.Contains(stage2).Should().BeTrue();
        }

        [Fact]
        public void CopyFlags_3Stages_FlagsAreCopiedOnlyFromThePreviousStage()
        {
            //Arrange
            var stage1 = Guid.NewGuid();
            _communityTest.AddFlag(0).AddQuestion(stage1, default, default);

            var stage2 = Guid.NewGuid();
            _communityTest.AddFlag(0).AddQuestion(stage2, default, default);

            //Act
            var stage3 = Guid.NewGuid();
            _communityTest.CopyFlags(stage2, stage3);

            //Assert
            var flagStage1 = _communityTest.GetFlags(stage1).Single();
            var flagStage3 = _communityTest.GetFlags(stage3).Single();
            
            flagStage1.Questions.Single().StageIds.Contains(stage3).Should().BeFalse();
            flagStage3.Questions.Single().StageIds.Contains(stage3).Should().BeTrue();
        }

        [Fact]
        public void RemoveFlagsThatDoNotBelongToAnyStage_QuestionWithStageId_IsNotDeleted()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var flag = _communityTest.AddFlag(0);
            flag.Questions.Add(new Question(stageId));

            //Act
            _communityTest.RemoveFlagsThatDoNotBelongToAnyStage();

            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(1);
            _communityTest.FlagsAllStages.Single().Questions.Single().StageIds.Contains(stageId).Should().BeTrue();
        }
        
        [Fact]
        public void RemoveFlagsThatDoNotBelongToAnyStage_QuestionWithoutStageId_IsDeleted()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var flag = _communityTest.AddFlag(0);
            flag.Questions.Add(new Question(stageId));
            flag.Questions.Single().RemoveFromStage(stageId);

            //Act
            _communityTest.RemoveFlagsThatDoNotBelongToAnyStage();

            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(0);
        }
        
        [Fact]
        public void RemoveFlagsThatDoNotBelongToAnyStage_TwoFlags_OnlyOneIsDeleted()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var stageId2 = Guid.NewGuid();
            var flag = _communityTest.AddFlag(0);
            flag.Questions.Add(new Question(stageId));
            flag.Questions.Single().RemoveFromStage(stageId);
            
            var flag2 = _communityTest.AddFlag(0);
            flag2.Questions.Add(new Question(stageId2));

            //Act
            _communityTest.RemoveFlagsThatDoNotBelongToAnyStage();

            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(1);
            _communityTest.FlagsAllStages.Single().Questions.Single().StageIds.Contains(stageId2).Should().BeTrue();
        }
        
        [Fact]
        public void RemoveFlagsThatDoNotBelongToAnyStage_TwoQuestions_OnlyOneIsDeleted()
        {
            //Arrange
            var stageId = Guid.NewGuid();
            var stageId2 = Guid.NewGuid();
            var flag = _communityTest.AddFlag(0);
            flag.Questions.Add(new Question(stageId));
            flag.Questions.Single().RemoveFromStage(stageId);
            
            flag.Questions.Add(new Question(stageId2));

            //Act
            _communityTest.RemoveFlagsThatDoNotBelongToAnyStage();

            //Assert
            _communityTest.FlagsAllStages.Count().Should().Be(1);
            _communityTest.FlagsAllStages.Single().Questions.Count.Should().Be(1);
            _communityTest.FlagsAllStages.Single().Questions.Single().StageIds.Contains(stageId2).Should().BeTrue();
        }
        
        [Fact]
        public void GetCurrentFlag_NonExistingQuestion_ThrowsArgumentException()
        {
            //Arrange
            var nonExistingQuestion = new Question(Guid.NewGuid());

            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _communityTest.GetCurrentFlag(nonExistingQuestion));
        }
        
        [Fact]
        public void GetCurrentFlag_QuestionBelongsToFlag_FlagIsReturned()
        {
            //Arrange
            var question = new Question(Guid.NewGuid());
            var flag = new Flag(0) { Questions = new List<Question> { question } };

            _communityTest.AddFlag(flag);
            
            //Act
            var flagReturned = _communityTest.GetCurrentFlag(question);

            //Assert
            flagReturned.Should().NotBeNull();
            flagReturned.Should().BeEquivalentTo(flag);
        }
        
        [Fact]
        public void GetCurrentFlag_SeveralFlags_CorrectFlagIsReturned()
        {
            //Arrange

            // 1st flag
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { new Question(Guid.NewGuid()) } });
            
            // 2nd flag
            var question = new Question(Guid.NewGuid());
            var flag = new Flag(0)
            {
                Questions = new List<Question>
                {
                    new Question(Guid.NewGuid()),
                    question,
                    new Question(Guid.NewGuid())
                }
            };

            _communityTest.AddFlag(flag);
            
            // 3rd flag
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { new Question(Guid.NewGuid()) } });
            
            //Act
            var flagReturned = _communityTest.GetCurrentFlag(question);

            //Assert
            flagReturned.Should().NotBeNull();
            flagReturned.Should().BeEquivalentTo(flag);
        }
        
        [Fact]
        public void GetNextQuestion_StageIdIsEmpty_ThrowsException()
        {
            //Arrange

            //Act
            //Assert
            Assert.Throws<ArgumentException>(() => _communityTest.GetNextQuestion(default, new Question(default)));
        }
        
        [Fact]
        public void GetNextQuestion_QuestionIsNull_ThrowsException()
        {
            //Arrange

            //Act
            //Assert
            Assert.Throws<ArgumentNullException>(() => _communityTest.GetNextQuestion(Guid.NewGuid(), null));
        }
        
        [Fact]
        public void GetNextQuestion_OneQuestionOnly_NextQuestionIsNull()
        {
            //Arrange
            var question = new Question(Guid.NewGuid());
            var flag = new Flag(0) { Questions = new List<Question> { question } };

            _communityTest.AddFlag(flag);

            //Act
            var nextQuestion = _communityTest.GetNextQuestion(Guid.NewGuid(), question);

            //Assert
            nextQuestion.Should().BeNull();
        }
        
        [Fact]
        public void GetNextQuestion_TwoQuestions_NextQuestionIsReturned()
        {
            //Arrange
            var stage = Guid.NewGuid();
            var question1 = new Question(stage);
            var question2 = new Question(stage);
            var flag = new Flag(0) { Questions = new List<Question> { question1, question2 } };

            _communityTest.AddFlag(flag);

            //Act
            var nextQuestion = _communityTest.GetNextQuestion(stage, question1);

            //Assert
            nextQuestion.Should().BeEquivalentTo(question2);
        }
        
        [Fact]
        public void GetNextQuestion_QuestionsFromDifferentStages_CorrectQuestionIsReturned()
        {
            //Arrange
            var stage = Guid.NewGuid();
            var question = new Question(stage);
            var nextQuestion = new Question(stage);

            _communityTest.AddFlag(new Flag(0)
            {
                Questions = new List<Question>
                {
                    question,
                    new Question(Guid.NewGuid()), // should be skipped because StageId is different 
                    nextQuestion
                }
            });

            //Act
            var result = _communityTest.GetNextQuestion(stage, question);

            //Assert
            result.Should().BeEquivalentTo(nextQuestion);
        }
        
        [Fact]
        public void GetNextQuestion_TwoQuestionsInDifferentFlags_NextQuestionIsReturned()
        {
            //Arrange
            var stage = Guid.NewGuid();
            var question = new Question(stage);
            var nextQuestion = new Question(stage);
            
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { question } });
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { nextQuestion } });

            //Act
            var result = _communityTest.GetNextQuestion(stage, question);

            //Assert
            result.Should().BeEquivalentTo(nextQuestion);
        }

        [Fact]
        public void GetNextQuestion_FlagsCreatedInRandomOrder_FlagsOrderedByTimeMarker()
        {
            //Arrange
            var stage = Guid.NewGuid();
            var questionInTheMiddle = new Question(stage);
            _communityTest.AddFlag(new Flag(100) { Questions = new List<Question> { questionInTheMiddle } });
            
            var questionAtTheBeginning = new Question(stage);
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { questionAtTheBeginning } });

            //Act
            var result = _communityTest.GetNextQuestion(stage, questionAtTheBeginning);

            //Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(questionInTheMiddle);
        }

        [Fact]
        public void Flags_FlagsCreatedInRandomOrder_FlagsOrderedByTimeMarker()
        {
            //Arrange
            var questionInTheMiddle = new Question(Guid.NewGuid());
            _communityTest.AddFlag(new Flag(100) { Questions = new List<Question> { questionInTheMiddle } });

            var questionAtTheBeginning = new Question(Guid.NewGuid());
            _communityTest.AddFlag(new Flag(0) { Questions = new List<Question> { questionAtTheBeginning } });

            //Act
            //Assert
            _communityTest.FlagsAllStages.First().TimeMarker
                .Should()
                .BeLessThan(_communityTest.FlagsAllStages.Last().TimeMarker);
        }
    }
}