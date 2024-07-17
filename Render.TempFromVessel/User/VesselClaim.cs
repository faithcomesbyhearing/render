using System.Security.Claims;

namespace Render.TempFromVessel.User
{
    public class VesselClaim : Claim
    {
        public Guid RoleId { get; set; }

        public VesselClaim(string type, string value, Guid roleId = default) : base(type, value)
        {
            RoleId = roleId;
        }
    }
}