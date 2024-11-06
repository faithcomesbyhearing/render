namespace Render.TempFromVessel.User;

public static class RoleNameExtensions
{
    public static Guid GetRoleId(this RoleName roleName)
    {
        return RenderRolesAndClaims.GetRoleId(roleName);
    }
}