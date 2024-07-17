namespace Render.WebAuthentication
{
    public interface IAuthenticationApiWrapper : IDisposable
    {
        Task<AuthenticationApiWrapper.SyncGatewayUser> AuthenticateRenderUserForSyncAsync(string username, string password, Guid projectId);

        Task<AuthenticationApiWrapper.AuthenticationResult> AuthenticateUserAsync(string username, string password);

        Task<AuthenticationApiWrapper.RenderProjectDownloadObject> AuthenticateForProjectDownloadViaIdAsync(string projectId);
    }
}