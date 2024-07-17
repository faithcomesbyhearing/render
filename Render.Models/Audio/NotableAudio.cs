using DynamicData;
using Newtonsoft.Json;
using Render.Models.Sections;

namespace Render.Models.Audio
{
    public class NotableAudio : Audio
    {
        [JsonProperty("CreatedFromAudioId")]
        public Guid CreatedFromAudioId { get; set; }
        
        [JsonProperty("Conversations")]
        public List<Conversation> Conversations { get; set; } = new List<Conversation>();
        
        public NotableAudio(Guid scopeId, Guid projectId, Guid parentId, int documentVersion = 1) : 
            base(scopeId, projectId, parentId, documentVersion)
        {
        }

        public void AddConversation(Conversation conversation)
        {
            Conversations.Add(conversation);
        }
        
        public void ClearConversations()
        {
            Conversations.Clear();
        }

        public List<Message> GetMessages()
        {
            return Conversations.SelectMany(x => x.Messages).ToList();
        }

        
        private void RemoveConversation(Conversation conversation)
        {
            var conversationId = conversation.Id;
            var index = Conversations.FindIndex(x => x.Id == conversationId);
            Conversations.RemoveAt(index);
        }

        public void UpdateOrDeleteConversation(Conversation updatedConversation)
        {
            //If we remove all messages from conversation. Conversation will be removed.
            if (updatedConversation.Messages.Count == 0)
            {
                RemoveConversation(updatedConversation);
                return;
            }
            
            //Otherwise remove original conversation with updated conversation 
            var conversationId = updatedConversation.Id;
            var index = Conversations.FindIndex(x => x.Id == conversationId);
            if(index < 0)
            {
                AddConversation(updatedConversation);
                return;
            }

            var originalConversation = Conversations[index];
            Conversations.Replace(originalConversation, updatedConversation);
        }
    }
}