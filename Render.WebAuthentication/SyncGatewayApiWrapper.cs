using Render.TempFromVessel.Kernel;
using System.Net;
using Render.Interfaces;

namespace Render.WebAuthentication
{
    public class SyncGatewayApiWrapper : ISyncGatewayApiWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly string _requestString;
        private readonly IRenderLogger _logger;

        public SyncGatewayApiWrapper(IAppSettings appSettings, HttpClient httpClient, IRenderLogger logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
            _requestString = $"https://{appSettings.CouchbaseReplicationUri}:{appSettings.ReplicationPort}/";
            _logger = logger;
        }

        public virtual async Task<bool> IsConnected()
        {
#if DEMO
            return false;
#endif
            try
            {
                var result = await _httpClient.GetAsync(_requestString);
                return result.StatusCode != HttpStatusCode.RequestTimeout;
            }
            //Likely a timeout exception, thus we're not connected
            catch (TaskCanceledException ex)
            {
                _logger.LogInfo("Exception occured", new Dictionary<string, string>
                {
                    { "Message", ex.Message }
                });

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}