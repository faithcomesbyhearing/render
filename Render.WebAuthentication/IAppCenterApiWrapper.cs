namespace Render.WebAuthentication
{
    public interface IAppCenterApiWrapper
    {
        string DownloadUrl { get; }

        Task<string> GetLatestVersionAsync(bool availableBetaTesting);
    }
}