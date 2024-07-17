using Render.Kernel;
using Environment = Android.OS.Environment;

namespace Render.Platforms.Kernel
{
    public class DownloadService : IDownloadService
    {
        private string SelectedPath { get; set; }

        public Task<string> ChooseFilePathAsync()
        {
            var baseP = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads);
            var rootPath = baseP?.AbsolutePath;

            SelectedPath = rootPath;
            return Task.FromResult(SelectedPath);
        }

        public async Task DownloadAsync(byte[] fileData, string fileName)
        {
            if (string.IsNullOrEmpty(SelectedPath))
            {
                return;
            }

            var fullPath = Path.Combine(SelectedPath, fileName);

            await using var writer = File.Create(fullPath);
            await writer.WriteAsync(fileData);
        }

        public async Task DownloadAsync(Stream stream, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}