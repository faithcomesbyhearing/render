namespace Render.TempFromVessel.User
{
	/// <summary>
	/// The name of a role in a project
	/// </summary>
	public enum RoleName
    {
	    // Assigned at the moment when the User is assigned to a Project for the first time.
	    // Stored in User.RoleIds and in User.Claims per specific ProjectID.
	    General,

	    // Stored in User.RoleIds and in User.Claims per specific ProjectID.
	    Configure,

	    // Stored in User.RoleIds and in User.Claims per specific ProjectID.
	    Consultant,
    }
}