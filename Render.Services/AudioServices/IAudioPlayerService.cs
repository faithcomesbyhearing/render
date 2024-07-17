using Render.Models.Audio;
using Render.Models.Sections;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Services.AudioServices
{
    public interface IAudioPlayerService: IDisposable
    {
        AudioPlayerState AudioPlayerState { get; }

        double Duration { get; }

        double CurrentPosition { get; }

        IAudioPlayer SimpleAudioPlayer { get; }

        event Action OnPlayerEnd;

        /// <summary>
        /// Loads the audio.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="timeMarkers">The audio TimeMarkers.</param>
        /// <param name="dataIsWav">Should be skipped. Used only on Android platform.</param>
        void Load(Audio audio, TimeMarkers timeMarkers = default, bool dataIsWav = false);

        /// <summary>
        /// Loads the audio.
        /// </summary>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="timeMarkers">The audio TimeMarkers.</param>
        /// <param name="dataIsWav">Should be skipped. Used only on Android platform.</param>
        void Load(Stream audioStream, TimeMarkers timeMarkers = default, bool dataIsWav = false);

        void Play();

        void Pause();

        void Seek(double time);

        void Stop();

        void Unload();
        Task LoadAsync(Stream audioStream, TimeMarkers timeMarkers);
    }
}