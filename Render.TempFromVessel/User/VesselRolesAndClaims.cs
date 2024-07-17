namespace Render.TempFromVessel.User
{
    /// <summary>
    /// When creating a role, add it to the constructor for VesselRolesAndClaims.
    /// Be sure to use the VisualStudio "CreateGuid" tool to pass the Guid into the constructor for Role()
    /// When creating a claim, add it to the ClaimList class below, following the structure.
    /// Add that claim to the Claims property in the VesselRolesAndClaims class.
    /// This will allow our startup to create policies for each claim. The policy will be named the same as the claim value
    /// Add claims from ClaimList to keep it as DRY as possible.
    /// </summary>
    public static class VesselRolesAndClaims
    {
        public const string AdministrativeGroupUserClaimType = "administrativegroup";
        public const string ProjectUserClaimType = "project";

        public const string AdministrativeGroupPolicyName = "administrativegroup";
        public const string ProjectPolicyName = "project";
        public const string ProjectRolePolicyName = "projectrole";
        public static List<Role> Roles { get; set; }

        static VesselRolesAndClaims()
        {
            Roles = new List<Role>()
            {
                new Role(Guid.Parse("C92005F3-070B-4DF3-9D8A-FE648325B28D"))
                {
                    Name = RoleName.Administrator,
                    Title = "Administrator",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.ProjectEdit,
                        ClaimList.Scripting,
                        ClaimList.ScriptImport,
                        ClaimList.ScriptPrepare,
                        ClaimList.ScriptReview,
                        ClaimList.Recording,
                        ClaimList.RecordingSession,
                        ClaimList.ReaderManagement,
                        ClaimList.PostProcessing,
                        ClaimList.MixingAndEditing,
                        ClaimList.ReferenceSelect
                    }
                },
                new Role(Guid.Parse("AD61000A-9B4C-4740-869C-301DABF292C9"))
                {
                    Name = RoleName.Scripting,
                    Title = "Scripting",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.Scripting,
                        ClaimList.ScriptImport,
                        ClaimList.ScriptPrepare,
                        ClaimList.ReferenceSelect
                    }
                },
                new Role(Guid.Parse("911F0B21-B851-497A-8D5C-8C3C1781FB5E"))
                {
                    Name = RoleName.ProjectAdministrator,
                    Title = "Project Administrator",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.ProjectEdit,
                        ClaimList.Scripting,
                        ClaimList.ScriptImport,
                        ClaimList.ScriptPrepare,
                        ClaimList.ScriptReview,
                        ClaimList.Recording,
                        ClaimList.RecordingSession,
                        ClaimList.ReaderManagement,
                        ClaimList.PostProcessing,
                        ClaimList.MixingAndEditing,
                        ClaimList.ReferenceSelect,
                        ClaimList.ChapterReview
                    }
                },
                new Role(Guid.Parse("9DEB555F-4C3C-44CA-80B1-7F6C5BAE0121"))
                {
                    Name = RoleName.ScriptReview,
                    Title = "Script Review",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.Scripting,
                        ClaimList.ScriptImport,
                        ClaimList.ScriptPrepare,
                        ClaimList.ScriptReview,
                        ClaimList.ReferenceSelect
                    }
                },
                new Role(Guid.Parse("4CBB2154-8FA9-4FD9-A9B1-12589C44990E"))
                {
                    Name = RoleName.PostProcessing,
                    Title = "Post Processing",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.ReferenceSelect,
                        ClaimList.PostProcessing,
                        ClaimList.MixingAndEditing
                    }
                },
                new Role(Guid.Parse("4E87970A-F640-4BF0-8AFE-F7BF189C6557"))
                {
                    Name = RoleName.AudioReview,
                    Title = "Audio Review",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.PostProcessing,
                        ClaimList.MixingAndEditing,
                        ClaimList.ReferenceSelect
                    }
                },
                new Role(Guid.Parse("71C64466-030A-4F94-A800-7F0FB265A9EC"))
                {
                    Name = RoleName.RecordingTeam,
                    Title = "Recording Team",
                    Claims = new List<VesselClaim>()
                    {
                        ClaimList.ReaderManagement,
                        ClaimList.Recording,
                        ClaimList.RecordingSession,
                        ClaimList.ReferenceSelect,
                        ClaimList.ChapterReview
                    }
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