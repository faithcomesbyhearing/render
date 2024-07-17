namespace Render.Services.AudioServices
{
    public interface IAudioEncodingService
    {
        Task<byte[]> ConvertWavToOpusAsync(
            Stream wavStream, 
            int sampleRate, 
            int channelCount);

        byte[] ConvertWavToOpus(
            Stream wavStream, 
            int sampleRate, 
            int channelCount);

        void ConvertOpusToWav(
            byte[] opusData,
            int sampleRate, 
            int channelCount, 
            Stream outWavStream);

        void ConcatenateIntoWav(
            IEnumerable<byte[]> opusAudios, 
            int sampleRate, 
            int channelCount, 
            Stream outWavStream);
        
        Task ConcatenateIntoWavAsync(
            IEnumerable<byte[]> opusAudios, 
            int sampleRate, 
            int channelCount, 
            Stream outWavStream,
            CancellationToken cancellationToken);
        
        Task<List<string>> SplitOpus(
            byte[] opusData,
            string tempAudioDirectoryPath,
            int sampleRate,
            int channelCount,
            List<int> timeMarkersInMilliseconds);

        Task<List<string>> SplitOpus(byte[] opusData,
            string tempAudioDirectoryPath,
            int sampleRate,
            int channelCount,
            int timeMarkerInMilliseconds);
    }
}