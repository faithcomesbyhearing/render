namespace Render.Models.Audio
{
    public class AudioGroup
    {
        public AudioStepTypes AudioStepType { get; set; }
        public IEnumerable<Audio> Audios { get; set; }

        public AudioGroup(AudioStepTypes audioStepType, IEnumerable<Audio> audios)
        {
            AudioStepType = audioStepType;
            Audios = audios;
        }
    }
}
