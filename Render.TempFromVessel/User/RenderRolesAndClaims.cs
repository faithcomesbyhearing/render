namespace Render.TempFromVessel.User
{
    public static class RenderRolesAndClaims
    {
        public const string ProjectUserClaimType = "project";
        public const string AuthenticatedPolicyName = "isAuthenticated";

        public static List<Role> Roles { get; set; }

        static RenderRolesAndClaims()
        {
            Roles = new List<Role>
            {
                new Role(Guid.Parse("4C42ABB4-4CBA-4B39-950A-86C727564D13"))
                {
                    Name = RoleName.General,
                    Title = "None",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.Authenticated
                    }
                },
                new Role(Guid.Parse("3B19BD47-FD75-443C-BA25-E9715D734ED3"))
                {
                    Name = RoleName.Configure,
                    Title = "Configure",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.Authenticated
                    },
                    IsRestricted = true
                },
                new Role(Guid.Parse("8810E362-FEA2-4716-86F6-74EFBF03DB51"))
                {
                    Name = RoleName.Consultant,
                    Title = "Consultant",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.Authenticated
                    },
                    IsRestricted = true
                }
            };
        }

        public static Role GetRoleById(Guid id)
        {
            return Roles.FirstOrDefault(x => x.Id == id);
        }

        public static Role GetRoleByName(RoleName name)
        {
            return Roles.FirstOrDefault(x => x.Name == name);
        }

        public static Role GetRoleByName(string name)
        {
            return Roles.FirstOrDefault(x => x.Name.ToString().ToUpper() == name.ToUpper());
        }
    }
}