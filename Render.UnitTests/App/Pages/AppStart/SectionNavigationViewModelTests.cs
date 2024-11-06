using FluentAssertions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.App.Pages.AppStart
{
    public class SectionNavigationViewModelTests  : ViewModelTestBase
    {
        private Section _section;
        private SectionNavigationViewModel _vm;
        
        public SectionNavigationViewModelTests()
        {
            _section = Section.UnitTestEmptySection();
            foreach (var passage in _section.Passages)
            {
                passage.ChangeCurrentDraftAudio(passage.CurrentDraftAudio);
            }
            _vm = new SectionNavigationViewModel("", MockContextProvider.Object);
        }

        [Fact]
        public void IsAudioMissing_WhenNoAudioMissing_ReturnsFalse()
        {
            //Arrange
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.Draft);
            //Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public void IsAudioMissing_WhenAudioMissing_ReturnsTrue()
        {
            //Arrange
            _section.Passages.First().CurrentDraftAudio.SetAudio(null);
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.Draft);
            //Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsAudioMissing_ReferencesAudioMissing_ReturnsTrue()
        {
            //Arrange
            _section.References.First().SetAudio(null);
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.Draft);
            //Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsAudioMissing_WhenNoAudioMissing_AndCheckingCommunityForCommunityRevise_ReturnsFalse()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
				var communityTest = new CommunityTest(passage.CurrentDraftAudioId, Guid.Empty, Guid.Empty);
				var flag = new Flag(100);
                flag.AddQuestion(Guid.Empty, Guid.Empty, Guid.Empty);
                var question = flag.Questions.First();
                question.QuestionAudio.SetAudio(new byte[] {0,1,2});
                var response = new Response(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                response.SetAudio(new byte[] {0, 2, 3});
                question.AddResponse(response);
                communityTest.AddFlag(flag);
                var retell = new CommunityRetell(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                retell.SetAudio(new byte[] {2, 5, 2});
                communityTest.AddRetell(retell);
                passage.CurrentDraftAudio.SetCommunityTest(communityTest);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.CommunityRevise, true);
            //Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public void IsAudioMissing_WhenAudioMissing_AndCheckingCommunityForCommunityRevise_ReturnsTrue()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
				var communityTest = new CommunityTest(passage.CurrentDraftAudioId, Guid.Empty, Guid.Empty);
				var flag = new Flag(100);
                flag.AddQuestion(Guid.Empty, Guid.Empty, Guid.Empty);
                var question = flag.Questions.First();
                question.QuestionAudio.SetAudio(new byte[] {0,1,2});
                var response = new Response(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                response.SetAudio(new byte[] {0, 2, 3});
                question.AddResponse(response);
                communityTest.AddFlag(flag);
                var retell = new CommunityRetell(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                communityTest.AddRetell(retell);
                passage.CurrentDraftAudio.SetCommunityTest(communityTest);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.CommunityRevise, true);
            //Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsAudioMissing_WhenNoRelevantAudioMissing_AndCheckingCommunityForCommunityTest_ReturnsFalse()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
				var communityTest = new CommunityTest(passage.CurrentDraftAudioId, Guid.Empty, Guid.Empty);
				var flag = new Flag(100);
                flag.AddQuestion(Guid.Empty, Guid.Empty, Guid.Empty);
                var question = flag.Questions.First();
                question.QuestionAudio.SetAudio(new byte[] {0,1,2});
                var response = new Response(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                question.AddResponse(response);
                communityTest.AddFlag(flag);
                var retell = new CommunityRetell(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                communityTest.AddRetell(retell);
                passage.CurrentDraftAudio.SetCommunityTest(communityTest);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.CommunityTest, true);
            //Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public void IsAudioMissing_WhenAudioMissing_AndCheckingCommunityForCommunityTest_ReturnsTrue()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
				var communityTest = new CommunityTest(passage.CurrentDraftAudioId, Guid.Empty, Guid.Empty);
				var flag = new Flag(100);
                flag.AddQuestion(Guid.Empty, Guid.Empty, Guid.Empty);
                communityTest.AddFlag(flag);
                passage.CurrentDraftAudio.SetCommunityTest(communityTest);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.CommunityTest, true);
            //Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void IsAudioMissing_WhenNoAudioMissing_AndCheckingBackTranslations_ReturnsFalse()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
                var secondRetell = new RetellBackTranslation(Guid.Empty, Guid.Empty, 
                    Guid.Empty, Guid.Empty, Guid.Empty);
                secondRetell.SetAudio(new byte[] {1,4,8});
                passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio = secondRetell;
                var segmentBT = new SegmentBackTranslation(new TimeMarkers(0, 100), Guid.Empty,
                    Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                segmentBT.SetAudio(new byte[] {3,7,1});
                var secondSegmentBT = new RetellBackTranslation(Guid.Empty, Guid.Empty, 
                    Guid.Empty, Guid.Empty, Guid.Empty);
                secondSegmentBT.SetAudio(new byte[] {5,9,2});
                segmentBT.RetellBackTranslationAudio = secondSegmentBT;
                passage.CurrentDraftAudio.SegmentBackTranslationAudios.Add(segmentBT);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.ConsultantCheck, checkForBackTranslationAudio: true);
            //Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public void IsAudioMissing_WhenAudioMissing_AndCheckingBackTranslations_ReturnsTrue()
        {
            //Arrange
            foreach (var passage in _section.Passages)
            {
                passage.CurrentDraftAudio.RetellBackTranslationAudio.SetAudio(null);
                var secondRetell = new RetellBackTranslation(Guid.Empty, Guid.Empty, 
                    Guid.Empty, Guid.Empty, Guid.Empty);
                passage.CurrentDraftAudio.RetellBackTranslationAudio.RetellBackTranslationAudio = secondRetell;
                var segmentBT = new SegmentBackTranslation(new TimeMarkers(0, 100), Guid.Empty,
                    Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
                var secondSegmentBT = new RetellBackTranslation(Guid.Empty, Guid.Empty, 
                    Guid.Empty, Guid.Empty, Guid.Empty);
                segmentBT.RetellBackTranslationAudio = secondSegmentBT;
                passage.CurrentDraftAudio.SegmentBackTranslationAudios.Add(segmentBT);
            }
            //Act
            var result = _vm.IsAudioMissing(_section, RenderStepTypes.ConsultantCheck, checkForBackTranslationAudio: true);
            //Assert
            result.Should().BeTrue();
        }
    }
}