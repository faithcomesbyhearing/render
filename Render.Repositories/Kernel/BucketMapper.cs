using Render.Models;
using Render.Models.LocalOnlyData;
using Render.Models.Workflow;
using Render.TempFromVessel.AdministrativeGroups;
using Render.Models.Audio;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Snapshot;
using Render.Models.Users;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;

namespace Render.Repositories.Kernel
{
    /// <summary>
    /// When adding a new Entity or ValueObject to the solution that requires a repository, add it here.
    /// In the constructor, add your typeof(NewClass), and decide which bucket the Entity/VO belongs in.
    /// After adding to this, you no longer need to worry about what bucket your object lives in;
    /// IDataPersistence<T> will get the correct bucket based on the Type passed to it.
    /// NOTE: Don't try to add a new bucket. If a new bucket needs to be added, include the Couchbase experts
    /// to make sure it is added to the solution and the servers correctly.
    /// </summary>
    public class BucketMapper : IBucketMapper
    {
        private readonly string _renderAudioDatabase = Buckets.renderaudio.ToString();
        private readonly string _localOnlyDataDatabase = Buckets.localonlydata.ToString();
        private readonly string _renderDatabase = Buckets.render.ToString();


        public BucketMapper()
        {
            TypeToBucketMap = new Dictionary<Type, string>
            {
                {typeof(AdministrativeGroup), _renderDatabase},
                {typeof(User), _renderDatabase},
                {typeof(Project), _renderDatabase},
                {typeof(Role), _renderDatabase},
                {typeof(RenderProject), _renderDatabase},
                {typeof(Reference), _renderDatabase},
                {typeof(Snapshot), _renderDatabase},
                {typeof(SectionReferenceAudio), _renderAudioDatabase},
                {typeof(Draft), _renderAudioDatabase},
                {typeof(Models.Audio.Audio), _renderAudioDatabase},
                {typeof(LocalProjects), _localOnlyDataDatabase},
                {typeof(Section), _renderDatabase},
                {typeof(RenderWorkflow), _renderDatabase},
                {typeof(Conversation), _renderDatabase},
                {typeof(BackTranslation), _renderAudioDatabase},
                {typeof(RetellBackTranslation), _renderAudioDatabase},
                {typeof(SegmentBackTranslation), _renderAudioDatabase},
                {typeof(WorkflowStatus), _renderDatabase},
                {typeof(RenderLog), _renderDatabase},
                {typeof(UserMachineSettings), _localOnlyDataDatabase},
                {typeof(RenderUser), _renderDatabase},
                {typeof(UserProjectSession), _localOnlyDataDatabase},
                {typeof(MachineLoginState), _localOnlyDataDatabase},
                {typeof(NotableAudio), _renderAudioDatabase},
                {typeof(CommunityTest), _renderDatabase},
                {typeof(CommunityRetell), _renderAudioDatabase},
                {typeof(StandardQuestion), _renderAudioDatabase},
                {typeof(Response), _renderAudioDatabase},
                {typeof(RenderProjectStatistics), _renderDatabase},
                {typeof(Scope), _renderDatabase}
            };
        }

        private Dictionary<Type, string> TypeToBucketMap { get; }

        public string GetBucketName<T>()
        {
            return TypeToBucketMap[typeof(T)];
        }
    }
}