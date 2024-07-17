using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.User;

namespace Render.Models.Users
{
    /// <summary>
    /// Model class for the User entity.
    /// </summary>
    public class User : DomainEntity, IAggregateRoot, IUser
    {
        [JsonIgnore]
        public UserType UserType => UserType.Vessel;

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

        /// <summary>
        /// We do not store the actual password; but instead the hashed for of it, for security
        /// reasons.
        /// </summary>
        [JsonProperty("HashedPassword")]
        public string HashedPassword { get; set; }

        /// <summary>
        /// The email address of the user, by which he can be contacted. Administrative
        /// roles generally require that the user have a password.
        /// </summary>
        [JsonProperty("Email")]
        public string Email { get; set; }

        /// <summary>
        /// The locale in which the user interface should be displayed for this user.
        /// </summary>
        [JsonProperty("LocalId")]
        public string LocaleId { get; set; }

        /// <summary>
        /// Gets or sets the primary reference preference identifier.
        /// </summary>
        /// <value>
        /// The primary reference preference identifier.
        /// </value>
        [JsonProperty("PrimaryReferencePreferences")]
        private List<UserReferencePreference> PrimaryReferencePreferences { get; set; } =
            new List<UserReferencePreference>();

        /// <summary>
        /// Gets or sets the secondary reference preference identifier.
        /// </summary>
        /// <value>
        /// The secondary reference preference identifier.
        /// </value>
        [JsonProperty("SecondaryReferencePreferences")]
        private List<UserReferencePreference> SecondaryReferencePreferences { get; set; } =
            new List<UserReferencePreference>();

        /// <summary>
        /// whether to use secondary reference bible
        /// </summary>
        [JsonProperty("UseSecondaryReferenceBible")]
        public bool UseSecondaryReferenceBible { get; set; }

        [JsonProperty("RoleIds")] public List<Guid> RoleIds { get; set; } = new List<Guid>();

        [JsonProperty("Claims")] public List<VesselClaim> Claims { get; set; } = new List<VesselClaim>();
        
        [JsonIgnore]
        public byte[] UserIcon { get; } = Array.Empty<byte>();

        [JsonIgnore]
        public bool IsGridPassword { get; set; } = false;
        
        [JsonProperty("PrimaryOrganizationId")]
        public Guid PrimaryOrganizationId { get; set; }

        [JsonProperty("AssociatedOrganizationIds")]
        public List<Guid> AssociatedOrganizationIds { get; set; } = new List<Guid>();

        [JsonProperty("Region")]
        public string Region { get; set; }

        [JsonProperty("SyncGatewayLogin")]
        public string SyncGatewayLogin { get; set; }

        /// <summary>
        /// Initializes a new instance of the a user.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        /// <param name="username">The username is forced to lowercase if it is not null.</param>
        /// <param name="email">The email.</param>
        /// <param name="localeId">The locale identifier.</param>
        public User(string fullName, string username, string email = null, string localeId = null)
            : base(Version)
        {
            FullName = fullName;
            Username = username?.ToLower();
            Email = email;
            LocaleId = localeId;
        }

        public bool HasRole(Role role)
        {
            return RoleIds.Any(x => x == role.Id);
        }

        public bool HasRole(Guid roleId)
        {
            return RoleIds.Any(x => x == roleId);
        }

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

        public Guid GetReferencePreferenceIdByProjectId(Guid projectId, bool getSecondary = false)
        {
            if (getSecondary)
            {
                if (SecondaryReferencePreferences.Count > 0)
                {
                    var referencePreference = SecondaryReferencePreferences
                        .FirstOrDefault(x => x.ProjectId == projectId);
                    if (referencePreference != null)
                    {
                        return referencePreference.ReferencePreferenceId;
                    }
                }
            }

            if (PrimaryReferencePreferences.Count > 0)
            {
                var referencePreference = PrimaryReferencePreferences
                    .FirstOrDefault(x => x.ProjectId == projectId);
                if (referencePreference != null)
                {
                    return referencePreference.ReferencePreferenceId;
                }
            }

            return Guid.Empty;
        }

        public void SetReferencePreferenceIdByProjectId(Guid projectId, Guid referenceId, bool setSecondary = false)
        {
            if (setSecondary)
            {
                var currentReferencePreference =
                    SecondaryReferencePreferences.FirstOrDefault(x => x.ProjectId == projectId);
                if (currentReferencePreference == default)
                {
                    SecondaryReferencePreferences.Add(new UserReferencePreference(projectId, referenceId));
                }
                else
                {
                    currentReferencePreference.SetReferencePreferenceId(referenceId);
                }
            }
            else
            {
                var currentReferencePreference =
                    PrimaryReferencePreferences.FirstOrDefault(x => x.ProjectId == projectId);
                if (currentReferencePreference == default)
                {
                    PrimaryReferencePreferences.Add(new UserReferencePreference(projectId, referenceId));
                }
                else
                {
                    currentReferencePreference.SetReferencePreferenceId(referenceId);
                }
            }
        }
        
        public List<Guid> RoleIdsForProject(string claimType, string claimName)
        {
            return Claims.Where(x => x.Type == claimType && x.Value == claimName).Select(x => x.RoleId).ToList();
        }
        
        public List<Role> RolesForProject(string claimType, string claimName)
        {
            return Claims.Where(x => x.Type == claimType && x.Value == claimName).Select(x => RenderRolesAndClaims.GetRoleById(x.RoleId)).ToList();
        }
        
        private const int Version = 3;
    }
}