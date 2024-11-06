using System.Security.Claims;

namespace Render.TempFromVessel.User
{
    // TODO: rename to RenderClaim as it is already in Launchpad
    public class VesselClaim : Claim
    {
        public Guid RoleId { get; set; }

        public VesselClaim(string type, string value, Guid roleId = default) : base(type, value)
        {
            RoleId = roleId;
        }
    }
}