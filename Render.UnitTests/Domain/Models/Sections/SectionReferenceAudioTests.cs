using Render.Models.Sections;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Sections
{
    public class SectionReferenceAudioTests : TestBase
    {
        [Fact]
        public void SectionReferenceAudio_SetPassageReferences_SetsList()
        {
            //Arrange
            var sectionReferenceAudio = SetupSectionReferenceAudio(false);
            var passageReference1 = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 100));
            var passageReference2 = new PassageReference(new PassageNumber(2), new TimeMarkers(100, 200));
            //Act
            sectionReferenceAudio.SetPassageReferences(new List<PassageReference>{passageReference1, passageReference2});
            
            //Assert
            sectionReferenceAudio.PassageReferences.Count.Should().Be(2);
            sectionReferenceAudio.PassageReferences.Should().Contain(passageReference1);
            sectionReferenceAudio.PassageReferences.Should().Contain(passageReference2);
        }

        [Fact]
        public void GetPassageReference_Succeeds()
        {
            //Arrange
            var sectionReferenceAudio = SetupSectionReferenceAudio(true);
            
            //Act
            var passageReference = sectionReferenceAudio.GetPassageReference(1);
            
            //Assert
            passageReference.PassageNumber.Number.Should().Be(1);
            passageReference.TimeMarkers.Should().NotBeNull();
            passageReference.TimeMarkers.StartMarkerTime.Should().Be(0);
            passageReference.TimeMarkers.EndMarkerTime.Should().Be(100);
        }

        private SectionReferenceAudio SetupSectionReferenceAudio(bool withPassageReferences)
        {
            var sectionReferenceAudio = new SectionReferenceAudio(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);
            if (!withPassageReferences)
            {
                return sectionReferenceAudio;
            }
            var passageReference1 = new PassageReference(new PassageNumber(1), new TimeMarkers(0, 100));
            var passageReference2 = new PassageReference(new PassageNumber(2), new TimeMarkers(100, 200));
            sectionReferenceAudio.SetPassageReferences(new List<PassageReference>{passageReference1, passageReference2});
            return sectionReferenceAudio;
        }
        
        [Fact]
        public void PassageReference_Reset_DivisionPassages()
        {
            //Arrange
            var passageReferences = new List<PassageReference>()
            {
                new (new PassageNumber(1,1), new TimeMarkers(0, 200)),
                new (new PassageNumber(1,2), new TimeMarkers(200, 300)),
                new (new PassageNumber(2,1), new TimeMarkers(300, 400)),
                new (new PassageNumber(2,2), new TimeMarkers(400, 500))
            };
            var sectionReferenceAudio = SetupSectionReferenceAudio(false);
            sectionReferenceAudio.SetPassageReferences(passageReferences);
            
            //Act
            sectionReferenceAudio.ResetPassageReferencesWithDivisions();
            
            //Assert
            sectionReferenceAudio.PassageReferences.Count.Should().Be(2);
            sectionReferenceAudio.PassageReferences[0].PassageNumber.Should().Be(new PassageNumber(1, 0));
            sectionReferenceAudio.PassageReferences[0].TimeMarkers.Should().Be(new TimeMarkers(0,300));
            sectionReferenceAudio.PassageReferences[1].PassageNumber.Should().Be(new PassageNumber(2, 0));
            sectionReferenceAudio.PassageReferences[1].TimeMarkers.Should().Be(new TimeMarkers(300,500));
        }
    }
}