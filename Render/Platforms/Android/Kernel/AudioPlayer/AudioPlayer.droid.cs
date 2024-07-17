using Render.Services.AudioPlugins.AudioPlayer;
using System.Diagnostics;
using Uri = Android.Net.Uri;

namespace Render.Platforms.Kernel.AudioPlayer
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class AudioPlayer : IAudioPlayer
    {
        private static int index = 0;

        private string _path;
        private Android.Media.MediaPlayer _player;

        ///<Summary>
        /// Length of audio in seconds
        ///</Summary>
        public double Duration
        {
            get => _player == null ? 0 : _player.Duration / 1000.0;
        }

        public double TotalDuration => throw new NotImplementedException();

        ///<Summary>
        /// Current position of audio playback in seconds
        ///</Summary>
        public double CurrentPosition
        {
            get => _player == null ? 0 : (double)_player.CurrentPosition / 1000.0;
        }

        public double TotalCurrentPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        ///<Summary>
        /// Playback volume (0 to 1)
        ///</Summary>
        public double Volume
        {
            get => _volume;
            set
            {
                SetVolume(_volume = value, Balance);
            }
        }

        private double _volume = 0.5;

        ///<Summary>
        /// Balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right
        ///</Summary>
        public double Balance
        {
            get => _balance;
            set
            {
                SetVolume(Volume, _balance = value);
            }
        }

        private double _balance = 0;

        ///<Summary>
        /// Indicates if the currently loaded audio file is playing
        ///</Summary>
        public bool IsPlaying
        {
            get => _player?.IsPlaying is true;
        }

        ///<Summary>
        /// Continously repeats the currently playing sound
        ///</Summary>
        public bool Loop
        {
            get => _loop;
            set
            {
                _loop = value;
                if (_player != null)
                {
                    _player.Looping = _loop;
                }
            }
        }

        private bool _loop;

        ///<Summary>
        /// Indicates if the position of the loaded audio file can be updated
        ///</Summary>
        public bool CanSeek
        {
            get => _player == null ? false : true;
        }

        public double? StartTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public double? EndTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        ///<Summary>
        /// Raised when audio playback completes successfully 
        ///</Summary>
        public event EventHandler PlaybackEnded;

        /// <summary>
        /// Instantiates a new SimpleAudioPlayer
        /// </summary>
        public AudioPlayer()
        {
            _player = new Android.Media.MediaPlayer
            {
                Looping = Loop
            };

            _player.Completion += OnPlaybackEnded;
        }

        ///<Summary>
        /// Load wav or opus audio file as a stream
        ///</Summary>
        public async Task<bool> LoadAsync(Stream audioStream, bool isWav = false)
        {
            _player?.Reset();

            DeleteFile(_path);

            //cache to the file system
            _path = Path.Combine(
                path1: Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                path2: $"cache{index++}.{(isWav ? "wav" : "ogg")}");

            var fileStream = File.Create(_path);
            await audioStream.CopyToAsync(fileStream);
            fileStream.Close();

            try
            {
                _player?.SetDataSource(_path);
            }
            catch
            {
                try
                {
                    var context = Android.App.Application.Context;
                    _player?.SetDataSource(context, Uri.Parse(Uri.Encode(_path))!);
                }
                catch
                {
                    return false;
                }
            }

            return PreparePlayer();
        }

        ///<Summary>
        /// Load wav or mp3 audio file from the iOS Resources folder
        ///</Summary>
        public async Task<bool> LoadAsync(string fileName)
        {
            if (_player is null)
            {
                return false;
            }

            _player.Reset();

            var fileDescriptor = Android.App.Application.Context.Assets?.OpenFd(fileName);
            if (fileDescriptor is null)
            {
                return false;
            }

            await _player.SetDataSourceAsync(fileDescriptor.FileDescriptor!, fileDescriptor.StartOffset!, fileDescriptor.Length!);
            return PreparePlayer();
        }

        ///<Summary>
        /// Load wav or opus audio file as a stream
        ///</Summary>
        public bool Load(Stream audioStream, bool isWav = false)
        {
            _player?.Reset();

            DeleteFile(_path);

            //cache to the file system
            _path = Path.Combine(
                path1: Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                path2: $"cache{index++}.{(isWav ? "wav" : "ogg")}");

            var fileStream = File.Create(_path);
            audioStream.CopyTo(fileStream);
            fileStream.Close();

            try
            {
                _player?.SetDataSource(_path);
            }
            catch
            {
                try
                {
                    var context = Android.App.Application.Context;
                    _player?.SetDataSource(context, Uri.Parse(Uri.Encode(_path))!);
                }
                catch
                {
                    return false;
                }
            }

            return PreparePlayer();
        }

        ///<Summary>
        /// Load wav or mp3 audio file from the iOS Resources folder
        ///</Summary>
        public bool Load(string fileName)
        {
            _player?.Reset();

            var fileDescriptor = Android.App.Application.Context.Assets?.OpenFd(fileName);
            if (fileDescriptor is null)
            {
                return false;
            }

            _player?.SetDataSource(fileDescriptor.FileDescriptor, fileDescriptor.StartOffset, fileDescriptor.Length);
            return PreparePlayer();
        }

        public void Unload()
        {
            _player?.Reset();
        }

        private bool PreparePlayer()
        {
            _player?.Prepare();

            return _player == null ? false : true;
        }

        private void DeletePlayer()
        {
            Stop();

            if (_player != null)
            {
                _player.Completion -= OnPlaybackEnded;
                _player.Release();
                _player.Dispose();
                _player = null;
            }

            DeleteFile(_path);
            _path = string.Empty;
        }

        private void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) == false)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }
            }
        }

        ///<Summary>
        /// Begin playback or resume if paused
        ///</Summary>
        public void Play()
        {
            if (_player == null)
                return;

            if (IsPlaying)
            {
                Pause();
                Seek(0);
            }

            _player.Start();
        }

        ///<Summary>
        /// Stop playack and set the current position to the beginning
        ///</Summary>
        public void Stop()
        {
            if (!IsPlaying)
                return;

            Pause();
            Seek(0);
        }

        ///<Summary>
        /// Pause playback if playing (does not resume)
        ///</Summary>
        public void Pause()
        {
            _player?.Pause();
        }

        ///<Summary>
        /// Set the current playback position (in seconds)
        ///</Summary>
        public void Seek(double position)
        {
            if (CanSeek)
                _player?.SeekTo((int)(position * 1000D));
        }

        ///<Summary>
        /// Sets the playback volume as a double between 0 and 1
        /// Sets both left and right channels
        ///</Summary>
        private void SetVolume(double volume, double balance)
        {
            volume = Math.Max(0, volume);
            volume = Math.Min(1, volume);

            balance = Math.Max(-1, balance);
            balance = Math.Min(1, balance);

            // Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
            var left = Math.Cos(Math.PI * (balance + 1) / 4) * volume;
            var right = Math.Sin(Math.PI * (balance + 1) / 4) * volume;

            _player?.SetVolume((float)left, (float)right);
        }

        private void OnPlaybackEnded(object sender, EventArgs e)
        {
            PlaybackEnded?.Invoke(sender, e);

            //this improves stability on older devices but has minor performance impact
            // We need to check whether the player is null or not as the user might have dipsosed it in an event handler to PlaybackEnded above.
            if (_player != null && Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {
                _player.SeekTo(0);
                _player.Stop();
                _player.Prepare();
            }
        }

        private bool isDisposed = false;

        ///<Summary>
		/// Dispose SimpleAudioPlayer and release resources
		///</Summary>
       	protected virtual void Dispose(bool disposing)
        {
            if (isDisposed || _player == null)
                return;

            if (disposing)
                DeletePlayer();

            isDisposed = true;
        }

        ~AudioPlayer()
        {
            Dispose(false);
        }

        ///<Summary>
        /// Dispose SimpleAudioPlayer and release resources
        ///</Summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}