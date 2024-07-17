using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.TempFromVessel.Project
{
    public class Project : DomainEntity, IAggregateRoot
    {
	    /// <summary>
	    /// Initializes a new instance of the <see cref="Project" /> class.
	    /// </summary>
	    /// <param name="name">The name.</param>
	    /// <param name="referenceNumber">The reference number</param>
	    /// <param name="isoCode">The iso code.</param>
	    public Project(string name, string referenceNumber, string isoCode)
            : base(Version)
        {
            Name = name;
            ReferenceNumber = referenceNumber;
            IsoCode = isoCode;
            DomainEntityVersion = Version;
        }

	    /// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		[JsonProperty("Name")]
        public string Name { get; private set; }
        
        /// <summary>
        /// The id for the project itself, stored in ProjectId for channels and indexing.
        /// </summary>
        [JsonProperty("ProjectId")]
        public Guid ProjectId => Id;

        /// <summary>
        /// Gets or sets the reference number.
        /// </summary>
        /// <value>
        /// The reference number.
        /// </value>
        [JsonProperty("ReferenceNumber")]
        public string ReferenceNumber { get; private set; }

        /// <summary>
        /// Gets or sets the iso code.
        /// </summary>
        /// <value>
        /// The iso code.
        /// </value>
        [JsonProperty("IsoCode")]
        public string IsoCode { get; private set; }
        
        [JsonProperty("MediaId")]
        public string MediaId { get; private set; }

        [JsonProperty("WorkProjectId")]
        public Guid WorkProjectId { get; private set; }

        [JsonProperty("WorkProjectType")]
        public string WorkProjectType { get; private set; }

        /// <summary>
        /// Gets or sets the parent group identifier.
        /// </summary>
        /// <value>
        /// The parent group identifier.
        /// </value>
        [JsonProperty("ParentGroupId")]
        public Guid ParentGroupId { get; private set; }

        /// <summary>
        /// Gets or sets the workflow identifier.
        /// </summary>
        /// <value>
        /// The workflow identifier.
        /// </value>
        [JsonProperty("WorkflowId")]
        public Guid WorkflowId { get; private set; }


        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Project"/> is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("GlobalUserIds")]
        public List<Guid> GlobalUserIds { get; private set; } = new();

        [JsonProperty("HasDeployedScopes")]
        public bool HasDeployedScopes { get; private set; }

		[JsonProperty("IsBetaTester")]
		public bool IsBetaTester { get; set; }

		public void SetName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Name = name;
            }
        }

        public void SetReferenceNumber(string referenceNumber)
        {
            if (!string.IsNullOrEmpty(referenceNumber))
            {
                ReferenceNumber = referenceNumber;
            }
        }

        public void SetIsoCode(string iso)
        {
            if (!string.IsNullOrEmpty(iso))
            {
                IsoCode = iso;
            }
        }

        public void SetWorkProjectId(Guid workProjectId)
        {
            WorkProjectId = workProjectId;
        }

        public void SetParentGroupId(Guid parentGroupId)
        {
            ParentGroupId = parentGroupId;
        }

        public void SetWorkflowId(Guid workflowId)
        {
            WorkflowId = workflowId;
        }

		public void SetBetaTester(bool isBetaTester)
		{
			IsBetaTester = isBetaTester;
		}

		private const int Version = 6;
    }
}