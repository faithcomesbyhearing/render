namespace Render.Interfaces.EssentialsWrappers
{
    public interface IEssentialsWrapper
    {
        Task<bool> CheckForAudioPermissions();

        Task<bool> AskForAudioPermissions();

        Task<bool> AskForFileAccessPermissions();

        Task OpenBrowserAsync(string uri);

        void InvokeOnMainThread(Action action);
    }
}