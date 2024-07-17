using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public enum ParentAudioType
    {
        Draft,
        PassageBackTranslation,
        SegmentBackTranslation,
        PassageBackTranslation2,
        SegmentBackTranslation2
    }
    
    public class Conversation : DomainEntity
    {
        [JsonProperty("Messages")]
        [Reactive]
        public List<Message> Messages { get; set; }

        [JsonProperty("Flag")]
        public double Flag { get; private set; }

        [JsonProperty("StageId")]
        public Guid StageId { get; private set; }

        [JsonIgnore]
        public ParentAudioType ParentAudioType { get; set; }
        [JsonIgnore]
        public Draft ParentAudio { get; set; }
        
        [JsonIgnore]
        public double FlagOverride { get; set; }
        
        public Conversation(double flag, Guid stageId) : base(1)
        {
            Flag = flag;
            FlagOverride = flag;
            StageId = stageId;
            Messages = new List<Message>();
        }

        public void SetFlag(double flag)
        {
            Flag = flag;
        }
        
        /// <summary>
        /// Returns true if conversation is to be deleted
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool DeleteMessage(Message message)
        {
            if (message == null)
            {
                return false;
            }

            var messageId = message.Id;
            var indexOfMessage = Messages.FindIndex(x => x.Id == messageId);
            Messages.RemoveAt(indexOfMessage);
            return Messages.Count == 0;
        }
    }
}