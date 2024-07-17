using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Users;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Models.Sections
{
    public class MessageTests : TestBase
    {
        [Fact]
        public void AddUserIdToSeenList_PreviouslySeenUserIdListEmpty_AddUserIdToList()
        {
            //Arrange
            var user = new User("Test User", "testuser");
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var media = new Media(audio.Id);
            var message = new Message(user.Id, media);
            var differentUser = new User("Diff User", "diffUser");
            
            //Act
            message.AddUserIdToSeenList(differentUser.Id); 

            //Assert
            message.PreviouslySeenUserIdList.Should().Contain(differentUser.Id);
        }
        
        [Fact]
        public void AddUserIdToSeenList_PreviouslySeenUserIdNotEmpty_DoNotAddUserToList()
        {
            //Arrange
            var user = new User("Test User", "testuser");
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var media = new Media(audio.Id);
            var message = new Message(user.Id, media);
            var differentUser = new User("Diff User", "diffUser");
            message.GetSeenStatus(differentUser.Id);

            //Assert 
            message.AddUserIdToSeenList(differentUser.Id); 
            
            //Assert
            message.PreviouslySeenUserIdList.Should().Contain(differentUser.Id);
            message.PreviouslySeenUserIdList.Count.Should().Be(1);
            message.NeedsInterpretation.Should().BeFalse();
        }
        
        [Fact]
        public void AddUserIdToSeenList_PreviouslySeenUserIdNotEmpty_HasNoUserId_AddUserIdFromList()
        {
            //Arrange
            var user = new User("Test User", "testuser");
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var media = new Media(audio.Id);
            var message = new Message(user.Id, media);
            var differentUser = new User("Diff User", "diffUser");
            message.AddUserIdToSeenList(differentUser.Id);
            
            //Act
            message.AddUserIdToSeenList(user.Id);

            //Assert
            message.PreviouslySeenUserIdList.Should().Contain(user.Id);
            message.PreviouslySeenUserIdList.Count.Should().Be(2);
            message.NeedsInterpretation.Should().BeTrue();
        }

        [Fact]
        public void Message_NoAudio_NeedsInterpretation_False()
        {
            //Arrange
            var user = new User("Test User", "testuser");
            var media = new Media(Guid.Empty);
            
            //Act
            var message = new Message(user.Id, media);
            
            //Assert
            message.NeedsInterpretation.Should().BeFalse();
        }
        
        [Fact]
        public void Message_HasAudio_NeedsInterpretation_True()
        {
            //Arrange
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var user = new User("Test User", "testuser");
            var media = new Media(audio.Id);
            
            //Act
            var message = new Message(user.Id, media);
            
            //Assert
            message.NeedsInterpretation.Should().BeTrue();
        }
        
        [Fact]
        public void AddUserIdToSeenList_NoAudio_NeedsInterpretation_False()
        {
            //Arrange
            var user = new User("Test User", "testuser");
            var media = new Media(Guid.Empty);
            var message = new Message(user.Id, media);

            //Act
            message.AddUserIdToSeenList(user.Id);
            //Assert
            message.NeedsInterpretation.Should().BeFalse();
        }
        
        [Fact]
        public void AddUserIdToSeenList_AuthorOfMessageIsDifferentFromAddedUserId_False()
        {
            //Arrange
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var user = new User("Test User", "testuser");
            var media = new Media(audio.Id);
            var message = new Message(user.Id, media);
            var differentUser = new User("Diff User", "diffUser");

            //Act
            message.AddUserIdToSeenList(differentUser.Id);
            //Assert
            message.NeedsInterpretation.Should().BeFalse();
        } 
        
        [Fact]
        public void AddUserIdToSeenList_AuthorOfMessageIsSame_True()
        {
            //Arrange
            var audio = new Audio(Guid.Empty, Guid.Empty, Guid.Empty);
            var user = new User("Test User", "testuser");
            var media = new Media(audio.Id);
            var message = new Message(user.Id, media);
            //Act
            message.AddUserIdToSeenList(user.Id);
            //Assert
            message.NeedsInterpretation.Should().BeTrue();
        }
    }
}