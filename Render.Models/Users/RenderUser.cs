using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.User;

namespace Render.Models.Users
{
    public class RenderUser : ProjectDomainEntity, IUser
    {
        [JsonIgnore]
        public UserType UserType => UserType.Render;
        
        /// <summary>
        /// The full name of the person; enough that a project or group administrator can
        /// identify the person within the context of the software application.
        /// </summary>
        [JsonProperty("FullName")]
        public string FullName { get; set; }

        /// <summary>
        /// The username is unique across the entire system, and is used for logging a user in.
        /// </summary>
        [JsonProperty("Username")]
        public string Username { get; set; }
        
        [JsonProperty("UserIcon")]
        public byte[] UserIcon { get; private set; } = Array.Empty<byte>();
        
        [JsonProperty("HashedPassword")]
        public string HashedPassword { get; set; }
        
        [JsonProperty("IsGridPassword")]
        public bool IsGridPassword { get; set; } = true;
        
        [JsonProperty("RoleIds")] public List<Guid> RoleIds { get; set; } = new List<Guid>();

        [JsonProperty("Claims")] public List<VesselClaim> Claims { get; set; } = new List<VesselClaim>();
        
        [JsonProperty("SyncGatewayLogin")]
        public string SyncGatewayLogin { get; set; }
        
        [JsonProperty("UserSyncCredentials")]
        public UserSyncCredentials UserSyncCredentials { get; set; }
        
        /// <summary>
        /// The locale in which the user interface should be displayed for this user.
        /// </summary>
        [JsonProperty("LocalId")]
        public string LocaleId { get; set; }


        public RenderUser(string fullName, Guid projectId) : base(projectId, Version)
        {
            FullName = fullName;
            Username =  fullName;
        }
        
        public const int Version = 3;
        
        public bool HasClaim(string claimType, string claimName = null, Guid roleId = default)
        {
            if (roleId != default)
            {
                return claimName == null
                    ? Claims.Any(x => x.Type == claimType && x.RoleId == roleId)
                    : Claims.Any(x => x.Type == claimType && x.Value == claimName && x.RoleId == roleId);
            }

            return claimName == null
                ? Claims.Any(x => x.Type == claimType)
                : Claims.Any(x => x.Type == claimType && x.Value == claimName);
        }
        
        public List<Guid> RoleIdsForProject(string claimType, string claimName)
        {
            return Claims.Where(x => x.Type == claimType && x.Value == claimName).Select(x => x.RoleId).ToList();
        }
        
        public List<RoleName> RolesForProject(string claimType, string claimName)
        {
            return Claims
                .Where(x => x.Type == claimType && x.Value == claimName)
                .Select(x => RenderRolesAndClaims.GetRoleName(x.RoleId))
                .ToList();
        }

        public void SetUserIcon(byte[] image)
        {
            UserIcon = image;
        }
        
        public bool HasRole(Role role)
        {
            return RoleIds.Any(x => x == role.Id);
        }

        public bool HasRole(Guid roleId)
        {
            return RoleIds.Any(x => x == roleId);
        }
    }
}