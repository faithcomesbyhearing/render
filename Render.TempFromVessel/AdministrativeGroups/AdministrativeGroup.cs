using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.TempFromVessel.AdministrativeGroups
{
    /// <summary>
    /// An Administrative Group is an organizational container by which items are managed.
    /// An administrator will manage the users within the group, and may also create and
    /// manage Administrative Groups as child groups. The administrator may also manage
    /// projects (e.g., Vessel Project, Render Project.)
    /// </summary>
    public class AdministrativeGroup : DomainEntity, IAggregateRoot
    {
        /// <summary>
        /// The name for this group, e.g., 'FCBH', 'Americas Area', 'Render'.
        /// </summary>
        /// <remarks>This simple implementation is not writing-system aware; it will only handle
        /// unicode that can be displayed in the system font. We anticipate that this will
        /// be sufficient for most of our administrators, who are the only ones that will
        /// likely view these groups.</remarks>
        [JsonProperty("Name")]
        public string Name { get; protected internal set; }

        [JsonProperty("ParentId")]
        public Guid ParentId { get; private set; }

        [JsonProperty("DigitalBiblePlatformLicenseKey")]
        public string DigitalBiblePlatformLicenseKey { get; private set; }

        public void UpdateParentId(Guid newParentId)
        {
            ParentId = newParentId;
        }

        public void SetDigitalBiblePlatformKey(string licenseKey)
        {
            DigitalBiblePlatformLicenseKey = licenseKey;
        }

        public void Rename(string newName)
        {
            this.Name = newName;
        }

        public AdministrativeGroup(string name)
            :base(1)
        {
            Name = name;
        }
    }
}