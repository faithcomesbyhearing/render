using Newtonsoft.Json;

namespace Render.TempFromVessel.Kernel
{
    /// <summary>
    /// Use this class, rather than DomainEntity, for objects that will be selectively synchronized
    /// by the Sync Gateway over a specific channel. The ProjectId is used by the Sync Gateway as its
    /// ChannelId, which indicates whether or not a given object should be synchronized to a given
    /// user's computer.
    /// </summary>
    /// <remarks>Those DomainEntities that to not have a repository, i.e., that are stored within
    /// their parent object, can be subclasses of DomainEntity rather than ProjectDomainEntity.</remarks>
    public class ProjectDomainEntity : DomainEntity
    {
        /// <summary>
        /// The id for the parent project in which this is a descendant Domain Entity.
        /// </summary>
        [JsonProperty("ProjectId")]
        public Guid ProjectId { get; private set; }

        protected ProjectDomainEntity(Guid projectId, int documentVersion)
            : base(documentVersion)
        {
            ProjectId = projectId;
        }
    }
}