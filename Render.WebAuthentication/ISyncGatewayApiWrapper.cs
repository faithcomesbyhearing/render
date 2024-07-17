namespace Render.WebAuthentication
{
    public interface ISyncGatewayApiWrapper
    {
        Task<bool> IsConnected();
    }
}
