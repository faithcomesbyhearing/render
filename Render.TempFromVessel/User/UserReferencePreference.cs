using Render.TempFromVessel.Kernel;

namespace Render.TempFromVessel.User
{
    public class UserReferencePreference : ValueObject
    {
        public Guid ProjectId { get; private set; }

        public Guid ReferencePreferenceId { get; private set; }

        public UserReferencePreference(Guid projectId, Guid referencePreferenceId)
        {
            ProjectId = projectId;
            ReferencePreferenceId = referencePreferenceId;
        }

        public void SetReferencePreferenceId(Guid referencePreferenceId)
        {
            ReferencePreferenceId = referencePreferenceId;
        }
    }
}