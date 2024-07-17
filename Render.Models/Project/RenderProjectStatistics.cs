using System.Globalization;
using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Project
{
    public class RenderProjectStatistics: ProjectDomainEntity, IAggregateRoot
    {
        public RenderProjectStatistics(Guid renderProjectId, Guid projectId) :base( projectId, Version)
        {
            RenderProjectId = renderProjectId;
            DomainEntityVersion = Version;
        }

        [JsonProperty("RenderProjectId")]
        public Guid RenderProjectId { get; private set; }

        [JsonProperty("FirstScopeDeployedDate")]
        public DateTimeOffset FirstScopeDeployedDate { get; private set; }

        [JsonProperty("FirstSectionDraftedDate")]
        public DateTimeOffset FirstSectionDraftedDate { get; private set; }

        [JsonProperty("FirstScriptureApprovedDate")]
        public DateTimeOffset FirstScriptureApprovedDate { get; private set; }

        [JsonProperty("RenderProjectLastSyncDate")]
        public DateTimeOffset RenderProjectLastSyncDate { get; private set; }

        [JsonProperty("OldTestamentVersesCount")]
        public int OldTestamentVersesCount { get; private set;}

        [JsonProperty("OldTestamentVersesCompletedCount")]
        public int OldTestamentVersesCompletedCount { get; private set;}

        [JsonIgnore]
        public string OldTestamentCompletedPercentageString => OldTestamentVersesCompletedCount <= 0 ? "0%" : (100.0 * OldTestamentVersesCompletedCount / OldTestamentVersesCount).ToString("F0", CultureInfo.InvariantCulture)+"%";

        [JsonProperty("NewTestamentVersesCount")]
        public int NewTestamentVersesCount { get; private set;}

        [JsonProperty("NewTestamentVersesCompletedCount")]
        public int NewTestamentVersesCompletedCount { get; private set;}

        [JsonIgnore]
        public string NewTestamentCompletedPercentageString => NewTestamentVersesCompletedCount <= 0 ? "0%" : (100.0 * NewTestamentVersesCompletedCount / NewTestamentVersesCount).ToString("F0", CultureInfo.InvariantCulture)+"%";

        [JsonProperty("UndeployedScopesCount")]
        public int UndeployedScopesCount { get; private set; }

        [JsonProperty("ActiveScopesCount")]
        public int ActiveScopesCount { get; private set; }

        [JsonProperty("CompletedScopesCount")]
        public int CompletedScopesCount { get; private set; }

        public void SetRenderProjectId(Guid renderProjectId)
        {
            RenderProjectId = renderProjectId;
        }

        public void SetFirstScopeDeployedDate(DateTimeOffset dateScopeDeployed)
        {
            FirstScopeDeployedDate = dateScopeDeployed;
        }

        public void SetFirstSectionDraftedDate(DateTimeOffset dateDrafted)
        {
            FirstSectionDraftedDate = dateDrafted;
        }

        public void SetFirstScriptureApprovedDate(DateTimeOffset dateScriptureApproved)
        {
            FirstScriptureApprovedDate = dateScriptureApproved;
        }

        public void SetRenderProjectLastSyncDate(DateTimeOffset syncDate)
        {
            RenderProjectLastSyncDate = syncDate;
        }

        public void SetOldTestamentVersesCount(int oldTestamentVersesCount)
        {
            OldTestamentVersesCount = oldTestamentVersesCount;
        }

        public void SetOldTestamentVersesCompletedCount(int oldTestamentVersesCompletedCount)
        {
            OldTestamentVersesCompletedCount = oldTestamentVersesCompletedCount;
        }

        public void SetNewTestamentVersesCompletedCount(int newTestamentVersesCompletedCount)
        {
            NewTestamentVersesCompletedCount = newTestamentVersesCompletedCount;
        }

        public void SetNewTestamentVersesCount(int newTestamentVersesCount)
        {
            NewTestamentVersesCount = newTestamentVersesCount;
        }

        public void SetUndeployedScopesCount(int undeployedScopesCount)
        {
            UndeployedScopesCount = undeployedScopesCount;
        }

        public void SetActiveScopesCount(int activeScopesCount)
        {
            ActiveScopesCount = activeScopesCount;
        }

        public void SetCompletedScopesCount(int completedScopesCount)
        {
            CompletedScopesCount = completedScopesCount;
        }

        private const int Version = 1;
    }
}