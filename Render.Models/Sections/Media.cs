using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class Media : ValueObject
    {
        [JsonProperty("AudioId")]
        public Guid AudioId { get; }

        [JsonProperty("Text")]
        public string Text { get; }

        [JsonIgnore] public bool HasAudio => Audio != null && Audio.HasAudio;
        
        [Reactive]
        [JsonIgnore] public Audio.Audio Audio { get; set; }
        
        public Media(Guid audioId = default, string text = "")
        {
            AudioId = audioId;
            Text = text;
        }
    }
}