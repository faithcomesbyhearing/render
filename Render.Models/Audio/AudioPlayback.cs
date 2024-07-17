using System;
using System.Collections.Generic;
using System.Linq;

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

        public AudioPlayback(Audio audio)
        {
            AudioId = audio.Id;
            AudioSequenceList = new List<byte[]> { audio.Data };
        }

        public AudioPlayback(Guid audioId, byte[] audioData)
        {
            AudioId = audioId;
            AudioSequenceList = new List<byte[]> { audioData };
        }

        public AudioPlayback(Guid audioId, IEnumerable<Audio> audioSequence)
        {
            AudioId = audioId;
            AudioSequenceList = audioSequence.Where(x => x?.Data?.Length > 0).Select(x => x.Data).ToList();
        }
    }
}