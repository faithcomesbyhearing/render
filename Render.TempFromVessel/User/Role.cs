namespace Render.TempFromVessel.User
{
    public class Role
    {
        public Role(Guid id)
        {
            Id = id;
        }

        public RoleName Name { get; set; }

        public string Title { get; set; }

        public List<VesselClaim> Claims { get; set; }

        public Guid Id { get; }

        public bool IsRestricted { get; set; } = true;
    }
}