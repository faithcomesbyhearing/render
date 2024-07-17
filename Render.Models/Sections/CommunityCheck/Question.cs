using Newtonsoft.Json;
using Render.Models.Audio;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections.CommunityCheck
{
    public class Question : DomainEntity
    {
        [JsonIgnore]
        public NotableAudio QuestionAudio { get; set; }
        
        [JsonIgnore]
        private List<Response> _responses { get; } = new List<Response>();

        [JsonIgnore]
        public IReadOnlyList<Response> Responses => _responses.AsReadOnly();

        [JsonProperty("StageIds")]
        private List<Guid> _stageIds { get; } = new List<Guid>();
        
        [JsonProperty("QuestionAudioId")]
        public Guid QuestionAudioId { get; set; }
        
        [JsonIgnore]
        public IReadOnlyList<Guid> StageIds => _stageIds.AsReadOnly();

        public Question(Guid stageId)
            : base(Version)
        {
            // ignore empty Guid when constructor is called by Serializer
            if (stageId != default)
            {
                _stageIds.Add(stageId);
            }
        }

        public void UpdateAudio(NotableAudio audio)
        {
            QuestionAudio = audio;
            QuestionAudioId = audio.Id;
        }

        public void UpdateAudio(byte[] audioData)
        {
            QuestionAudio.SetAudio(audioData);
        }

        public void AddStage(Guid stageId)
        {
            if (stageId == default)
            {
                throw new ArgumentException(nameof(stageId));
            }

            if (_stageIds.Contains(stageId)) return;
            
            _stageIds.Add(stageId);
        }

        public void RemoveFromStage(Guid stageId)
        {
            _stageIds.Remove(stageId);
        }
        
        public void RemoveFromStage(IEnumerable<Guid> stageIds)
        {
            foreach (var stageId in stageIds)
            {
                RemoveFromStage(stageId);
            }
        }

        public void AddResponse(Response audio)
        {
            _responses.Add(audio);
        }
        
        private const int Version = 1;

        public void AddResponses(List<Response> responses)
        {
            _responses.AddRange(responses);
        }
    }
}