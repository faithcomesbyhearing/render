using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class UserMachineSettings : DomainEntity
    {
        private const int _documentVersion = 0;
        
        [JsonProperty("LastSelectedProjectId")]
        private Guid LastSelectedProjectId { get; set; }
        
        [JsonProperty("UserId")]
        private Guid UserId { get; }
        
        /// <summary>
        ///   Default font size in the transcription window
        /// </summary>
        [JsonProperty("TranscribeFontSize")]
        public double TranscribeFontSize { get; private set; }

        public Guid GetLastSelectedProjectId()
        {
            return LastSelectedProjectId;
        }

        public void SetFontSize(double fontSize)
        {
            TranscribeFontSize = fontSize;
        }

        public bool GetAndSetLastSelectedProject(Guid projectId)
        {
            var needsUpdate = LastSelectedProjectId != projectId;
            LastSelectedProjectId = projectId;
            return needsUpdate;
        }

        public UserMachineSettings(Guid userId) : base(_documentVersion)
        {
            UserId = userId;
        }
    }
}