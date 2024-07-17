using Render.Models.Audio;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow.Stage;
using Render.TempFromVessel.Project;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Domain.Audios
{
    public class NotableAudioTests : TestBase
    {
        private Scope _scope;
        private Project _project;
        private Stage _stage;
        private User _user;
        
        public NotableAudioTests()
        {
            _project = new Project("project name", "ref number", "iso code");
            _scope = new Scope(_project.Id);
            _user = new User("full name", "username");
            _stage = new Stage();
        }

        [Fact]
        public void UpdateOrDeleteConversation_NoMessages_DeleteConversationFromConversationList()
        {
            //Arrange
            var message = new Message(_user.Id, new Media(text: "Message"));
            var conversation = new Conversation(100, _stage.Id);
            conversation.Messages.Add(message);
            conversation.DeleteMessage(message);
            var notableAudio = new NotableAudio(_scope.Id, _project.Id, Guid.NewGuid());
            notableAudio.AddConversation(conversation);  
            
            //Act
            notableAudio.UpdateOrDeleteConversation(conversation);

            //Assert
            notableAudio.Conversations.Should().BeEmpty();
        }
        
        [Fact]
        public void UpdateOrDeleteConversation_StillHaveMessages_UpdateConversationList()
        {
            //Arrange
            var message = new Message(_user.Id, new Media(text: "Message"));
            var message2 = new Message(_user.Id, new Media(text: "Message2"));
            var conversation = new Conversation(100, _stage.Id);
            conversation.Messages.AddRange(new List<Message>(){message, message2});
            conversation.DeleteMessage(message);
            var notableAudio = new NotableAudio(_scope.Id, _project.Id, Guid.NewGuid());
            notableAudio.AddConversation(conversation);

            //Act
            notableAudio.UpdateOrDeleteConversation(conversation);

            //Assert
            notableAudio.Conversations.Should().Contain(conversation);
        }
    }
}