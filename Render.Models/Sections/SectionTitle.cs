using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class SectionTitle : ValueObject
    {
        [JsonProperty("Text")]
        public string Text { get; }
        
        [JsonProperty("AudioId")]
        public Guid AudioId { get; }
        
        [JsonProperty("LanguageId")]
        public Guid LanguageId { get; }
        
        [JsonProperty("FailedVerification")]
        public bool FailedVerification { get; private set; }
        
        [JsonIgnore]
        public Audio.Audio Audio { get; private set; }

        public SectionTitle(string text, Guid audioId, Guid languageId = default)
        {
            Text = text;
            AudioId = audioId;
            LanguageId = languageId;
        }

        public void SetAudio(Audio.Audio audio)
        {
            Audio = audio;
        }
    }
}