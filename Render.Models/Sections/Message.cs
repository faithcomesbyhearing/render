using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class Message : DomainEntity
    {
        [JsonProperty("UserId")]
        public Guid UserId { get; private set; } //User who created the message
        
        [JsonProperty("InterpreterUserId")]
        public Guid InterpreterUserId { get; private set; }

        [JsonProperty("PreviouslySeenUserIdList")]
        public List<Guid> PreviouslySeenUserIdList { get; private set; }

        [JsonProperty("NeedsInterpretation")]
        public bool NeedsInterpretation { get; private set; }

        [JsonProperty("Media")]
        public Media Media { get; private set; }

        [Reactive]
        [JsonIgnore] public Audio.Audio InterpretationAudio { get; set; }

        public Message(Guid userId, Media media) : base(2)
        {
            UserId = userId;
            Media = media;
            PreviouslySeenUserIdList = new List<Guid>();
            NeedsInterpretation = media.AudioId != Guid.Empty;
        }
        
        public void CompleteInterpretation()
        {
            NeedsInterpretation = false;
        }

        public bool GetSeenStatus(Guid userId)
        {
            return PreviouslySeenUserIdList.Contains(userId);
        }

        public void AddUserIdToSeenList(Guid userId)
        {
            NeedsInterpretation = UserId == userId && Media.AudioId != Guid.Empty;
            
            var status = GetSeenStatus(userId);
            if (!status)
            {
                PreviouslySeenUserIdList.Add(userId);
            }
        }

        public void SetInterpreterUserId(Guid interpreterUserId)
        {
            InterpreterUserId = interpreterUserId;
        }
    }
}