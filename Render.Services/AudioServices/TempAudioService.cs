using ReactiveUI;
using Render.Interfaces;
using Render.Models.Audio;
using System.Reactive.Linq;

namespace Render.Services.AudioServices
{
    public class TempAudioService : ITempAudioService
    {
        /// <summary>
        /// String key for destinguishing temp audios between application 
        /// sessions to delete temp files from the previous launch only.
        /// </summary>
        private static readonly string _sessionKey;
        
        static TempAudioService()
        {
            _sessionKey = Random.Shared.Next(10000, 100000).ToString();
        }

        private readonly IAudioEncodingService _audioEncodingService;
        private readonly IAppDirectory _appDirectory;
        private AudioPlayback _audio;

        private string _tempAudioPath;
        private IDisposable _disposable;

        public TempAudioService(Audio audio, string audioPath, bool mutable, IAppDirectory appDirectory, IAudioEncodingService audioEncodingService)
        {
            _audio = new AudioPlayback(audio);
            _appDirectory = appDirectory;
            _audioEncodingService = audioEncodingService;

            _tempAudioPath = audioPath ?? GenerateAudioPath();

            if (mutable)
            {
                _disposable = audio
                    .WhenAnyValue(a => a.Data)
                    .Skip(1)
                    .Subscribe(data => { _tempAudioPath = GenerateAudioPath(); });
            }
        }

        public TempAudioService(AudioPlayback audio, IAppDirectory appDirectory, IAudioEncodingService audioEncodingService)
        {
            _audio = audio;
            _appDirectory = appDirectory;
            _audioEncodingService = audioEncodingService;
            _tempAudioPath = GenerateAudioPath();
        }

        public string SaveTempAudio()
        {
            if (File.Exists(_tempAudioPath))
            {
                return _tempAudioPath;
            }

            using (Stream fsWav = new FileStream(_tempAudioPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                _audioEncodingService.ConcatenateIntoWav(_audio.AudioSequenceList, 48000, 1, fsWav);
            }

            return _tempAudioPath;
        }

        public async Task<string> SaveTempAudioAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(_tempAudioPath))
            {
                return _tempAudioPath;
            }

            var unfinishedTempAudioPath = Path.Combine(Path.GetDirectoryName(_tempAudioPath), $"{Guid.NewGuid()}.wav.tmp");

            await using (Stream fsWav = new FileStream(unfinishedTempAudioPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                try
                {
                    await _audioEncodingService.ConcatenateIntoWavAsync(_audio.AudioSequenceList, 48000, 1, fsWav, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    fsWav.Close();
                    File.Delete(unfinishedTempAudioPath);

                    return null;
                }
            }

            if (!File.Exists(_tempAudioPath))
            {
                File.Move(unfinishedTempAudioPath, _tempAudioPath);
            }

            return _tempAudioPath;
        }

        public Stream OpenAudioStream()
        {
            if (!File.Exists(_tempAudioPath))
            {
               SaveTempAudio();
            }

            return new FileStream(_tempAudioPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        
        public async Task<Stream> OpenAudioStreamAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_tempAudioPath))
            {
                await SaveTempAudioAsync(cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return new FileStream(_tempAudioPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        private string GenerateAudioPath()
        {
            var fileName = GenerateAudioFileName();
            return Path.Combine(_appDirectory.TempAudio, fileName);
        }

        private string GenerateAudioFileName()
        {
            byte[] audioDataToHash = _audio.AudioSequenceList.Count == 1
                ? _audio.AudioSequenceList.Single()
                : _audio.AudioSequenceList.SelectMany(x => x).ToArray();

            var hash = ComputeHash(audioDataToHash);
            var fileName = $"{_audio.AudioId}_{audioDataToHash.Length}_{hash}_{_sessionKey}.wav";

            return fileName.Replace("-", string.Empty);
        }

        private string ComputeHash(byte[] data)
        {
            byte[] hash;
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                hash = md5.ComputeHash(data);
            }

            return BitConverter.ToString(hash);
        }

        public void Dispose()
        {
            _audio = null;
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}