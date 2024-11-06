namespace Render.TempFromVessel.User
{
    public static class RenderRolesAndClaims
    {
        public const string ProjectUserClaimType = "project";
        public const string AdministrativeGroupUserClaimType = "administrativegroup";

        private static readonly Dictionary<RoleName, Guid> RoleNameIds = new()
        {
            { RoleName.General, Guid.Parse("4C42ABB4-4CBA-4B39-950A-86C727564D13") },
            { RoleName.Configure, Guid.Parse("3B19BD47-FD75-443C-BA25-E9715D734ED3") },
            { RoleName.Consultant, Guid.Parse("8810E362-FEA2-4716-86F6-74EFBF03DB51") }
        };

        public static Guid GetRoleId(RoleName roleName)
        {
            return RoleNameIds[roleName];
        }

        public static RoleName GetRoleName(Guid roleId)
        {
            return RoleNameIds.Single(x => x.Value == roleId).Key;
        }

        public static bool TryGetRoleId(RoleName roleName, out Guid roleId)
        {
            return RoleNameIds.TryGetValue(roleName, out roleId);
        }

        public static bool TryGetRoleName(Guid roleId, out RoleName? roleName)
        {
            if (RoleNameIds.Any(x => x.Value == roleId))
            {
                roleName = GetRoleName(roleId);
                return true;
            }

            roleName = null;
            return false;
        }
    }
}