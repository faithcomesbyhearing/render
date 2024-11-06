namespace Render.Models.Audio
{
    /// <summary>
    /// Audio that can be played by Audio Player.
    /// Supports multiple audios that should be played sequentially as single whole audio.
    /// </summary>
    public class AudioPlayback
    {
        public Guid AudioId { get; }

        // Audio Sequence in OPUS format
        public List<byte[]> AudioSequenceList { get; }

        public bool HasAudioData => AudioSequenceList.Any(x => x?.Length > 0);

        /// <summary>
        /// Audio samples to draw wave form
        /// </summary>
        public float[] Samples { get; }

        public AudioPlayback(Audio audio)
        {
            AudioId = audio.Id;
            AudioSequenceList = new List<byte[]> { audio.Data };
            Samples = audio.PreviewSamples;
        }

        public AudioPlayback(Guid audioId, byte[] audioData, float[] samples = null)
        {
            AudioId = audioId;
            AudioSequenceList = new List<byte[]> { audioData };
            Samples = samples;
        }

        public AudioPlayback(Guid audioId, IEnumerable<Audio> audioSequence)
        {
            AudioId = audioId;
            AudioSequenceList = audioSequence.Where(x => x?.Data?.Length > 0).Select(x => x.Data).ToList();
            Samples = audioSequence.All(audio => audio?.PreviewSamples is not null) ?
                         audioSequence.SelectMany(audio => audio.PreviewSamples).ToArray() :
                         null;
        }
    }
}