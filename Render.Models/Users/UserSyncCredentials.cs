using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Users
{
    public class UserSyncCredentials : ValueObject
    {
        [JsonProperty("UserId")]
        public Guid UserId { get; set; }
        
        [JsonProperty("UserSyncGatewayLogin")]
        public string UserSyncGatewayLogin { get; set; }

        public UserSyncCredentials(Guid userId, string userSyncGatewayLogin)
        {
            UserId = userId;
            UserSyncGatewayLogin = userSyncGatewayLogin;
        }
    }
}