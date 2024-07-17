namespace Render.Services.AudioPlugins.AudioPlayer
{
    /// <summary>
    /// Interface for AudioPlayer
    /// </summary>
    public interface IAudioPlayer : IDisposable
    {
        ///<Summary>
        /// Raised when audio playback completes successfully 
        ///</Summary>
        event EventHandler PlaybackEnded;

        ///<Summary>
        /// Length of audio in seconds (considering StartTime & EndTime)
        ///</Summary>
        double Duration { get; }

        ///<Summary>
        /// Total length of audio in seconds (ignoring StartTime & EndTime)
        ///</Summary>
        double TotalDuration { get; }

        ///<Summary>
        /// Current position of audio playback in seconds (considering StartTime & EndTime)
        ///</Summary>
        double CurrentPosition { get; }

        ///<Summary>
        /// Current position of audio playback in seconds (ignoring StartTime & EndTime)
        ///</Summary>
        double TotalCurrentPosition { get; }

        ///<Summary>
        /// Playback volume 0 to 1 where 0 is no-sound and 1 is full volume
        ///</Summary>
        double Volume { get; set; }

        ///<Summary>
        /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
        ///</Summary>
        double Balance { get; set; }

        ///<Summary>
        /// Continously repeats the currently playing sound
        ///</Summary>
        bool Loop { get; set; }

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated
        ///</Summary>
        bool CanSeek { get; }

        ///<Summary>
        /// Indicates if the player is playing
        ///</Summary>
        bool IsPlaying { get; }

        ///<Summary>
        /// Load wav or opus audio file as a stream
        ///</Summary>
        bool Load(Stream audioStream, bool isWav = false);

        ///<Summary>
        /// Load wav or mp3 audio file from local path
        ///</Summary>
        bool Load(string fileName);

        ///<Summary>
        /// Load wav or opus audio file as a stream
        ///</Summary>
        Task<bool> LoadAsync(Stream audioStream, bool isWav = false);

        ///<Summary>
        /// Load wav or mp3 audio file from local path
        ///</Summary>
        Task<bool> LoadAsync(string fileName);

        /// <summary>
        /// Unload audio file from player
        /// </summary>
        void Unload();

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        void Play();

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        void Pause();

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        void Stop();

        ///<Summary>
        /// Set the current playback position (in seconds)
        ///</Summary>
        void Seek(double position);

        ///<Summary>
        /// Set start boundary to play audio (in seconds)
        ///</Summary>
        double? StartTime { get; set; }

        ///<Summary>
        /// Set end boundary to play audio (in seconds)
        ///</Summary>
        double? EndTime { get; set; }
    }
}