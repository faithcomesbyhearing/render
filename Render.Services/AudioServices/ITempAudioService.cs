namespace Render.Services.AudioServices
{
    public interface ITempAudioService : IDisposable
    {
        string SaveTempAudio();

        Task<string> SaveTempAudioAsync(CancellationToken cancellationToken);

        Stream OpenAudioStream();

        Task<Stream> OpenAudioStreamAsync(CancellationToken cancellationToken);
    }
}