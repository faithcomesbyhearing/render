using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Scope
{
    /// <summary>
    /// Use this class, rather than ProjectDomainEntity, for objects that will be selectively synchronized
    /// by the Sync Gateway over a specific channel by scope. The ScopeId is used by the Sync Gateway as its
    /// ChannelId, which indicates whether or not a given object should be synchronized to a given
    /// user's computer.
    /// </summary>
    /// <remarks>Those DomainEntities that do not have a repository, i.e., that are stored within
    /// their parent object, can be subclasses of DomainEntity rather than ScopeDomainEntity.</remarks>
    public class ScopeDomainEntity : ProjectDomainEntity
    {
        /// <summary>
        /// The id for the scope in which this document belongs.
        /// </summary>
        [JsonProperty("ScopeId")]
        public Guid ScopeId { get; private set; }

        protected ScopeDomainEntity(Guid scopeId, Guid projectId, int documentVersion)
            : base(projectId, documentVersion)
        {
            ScopeId = scopeId;
        }
    }
}