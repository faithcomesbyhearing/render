using Render.Interfaces.EssentialsWrappers;

namespace Render.Mocks.EssentialWrappers
{
    public class EssentialsWrapperMock : IEssentialsWrapper
    {
        private readonly IEssentialsWrapper _essentialsWrapper;

        public EssentialsWrapperMock(IEssentialsWrapper essentialsWrapper)
        {
            _essentialsWrapper = essentialsWrapper;
        }

        public Task<bool> CheckForAudioPermissions()
        {
            return Task.FromResult(true);
        }

        public Task<bool> AskForAudioPermissions()
        {
            return Task.FromResult(true);
        }
        
        public async Task OpenBrowserAsync(string uri)
        {
            await _essentialsWrapper.OpenBrowserAsync(uri);
        }

        public Task<bool> AskForFileAccessPermissions()
        {
            return _essentialsWrapper.AskForFileAccessPermissions();
        }

        public void InvokeOnMainThread(Action action)
        {
            _essentialsWrapper.InvokeOnMainThread(action);
        }
    }
}