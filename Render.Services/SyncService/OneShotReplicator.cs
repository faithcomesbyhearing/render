using System.Reactive.Linq;
using ReactiveUI;
using Render.Interfaces;
using Render.Interfaces.EssentialsWrappers;
using Render.Models.Sections;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Project;

namespace Render.Services.SyncService
{
    public class OneShotReplicator : ReactiveObject, IOneShotReplicator
    {
        private readonly IEssentialsWrapper _essentialsWrapper;
        private readonly List<IDisposable> _disposables = new();
        private bool _disposed;

        private IReplicatorWrapper RenderProjectsReplicator { get; }
        private IReplicatorWrapper RecordedAudioReplicator { get; }
        private IDataPersistence<Project> ProjectRepository { get; }
        private IDataPersistence<WorkflowStatus> WorkflowStatusRepository { get; }
        private List<string> ProjectChannels { get; }
        private List<string> UserChannels { get; } = new();

        private readonly IRenderLogger _logger;

        public event Action<bool> DownloadFinished;

        public OneShotReplicator(
            IRenderLogger logger,
            string connectionString,
            int maxSyncAttempts,
            string syncGatewayUsername,
            string syncGatewayPassword,
            List<Guid> projectChannels,
            List<Guid> userChannels,
            string databasePath,
            IDataPersistence<Project> projectRepository,
            IDataPersistence<WorkflowStatus> workflowStatusRepository, IEssentialsWrapper essentialsWrapper)
        {
            _essentialsWrapper = essentialsWrapper;
            _logger = logger;
            ProjectRepository = projectRepository;
            WorkflowStatusRepository = workflowStatusRepository;
            ProjectChannels = projectChannels.Select(x => x.ToString()).ToList();
            UserChannels.AddRange(userChannels.Select(x => x.ToString()));
            RenderProjectsReplicator = new ReplicatorWrapper(logger, databasePath);
            RecordedAudioReplicator = new ReplicatorWrapper(logger, databasePath);

            RenderProjectsReplicator.Configure(
                databaseName: Buckets.render.ToString(),
                connectionString: connectionString,
                maxSyncAttempts: maxSyncAttempts,
                syncGatewayUsername: syncGatewayUsername,
                syncGatewayPassword: syncGatewayPassword);

            RecordedAudioReplicator.Configure(
                databaseName: Buckets.renderaudio.ToString(),
                connectionString: connectionString,
                maxSyncAttempts: maxSyncAttempts,
                syncGatewayUsername: syncGatewayUsername,
                syncGatewayPassword: syncGatewayPassword);

            _disposables.Add(this.WhenAnyValue(x => x.RecordedAudioReplicator.Completed)
                .Subscribe(CheckForDownloadComplete));
            _disposables.Add(this.WhenAnyValue(x => x.RenderProjectsReplicator.Completed)
                .Subscribe(CheckForDownloadComplete));
            
            _disposables.Add(this.WhenAnyValue(x => x.RecordedAudioReplicator.Error)
                .Where(error => error)
                .Subscribe(_ => RaiseDownloadFinished(false)));
            _disposables.Add(this.WhenAnyValue(x => x.RenderProjectsReplicator.Error)
                .Where(error => error)
                .Subscribe(_ => RaiseDownloadFinished(false)));
        }

        public void StartDownload(bool freshDownload = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Replication cannot be started after disposal");
            }

            var renderBucketChannels = new List<string>();
            renderBucketChannels.AddRange(ProjectChannels);
            renderBucketChannels.AddRange(UserChannels);
            RenderProjectsReplicator.StartDownload(renderBucketChannels, freshDownload);
            RecordedAudioReplicator.StartDownload(ProjectChannels, freshDownload);
        }

        public void StopDownload()
        {
            RecordedAudioReplicator.Stop();
            RenderProjectsReplicator.Stop();
        }

        private void RaiseDownloadFinished(bool success)
        {
            DownloadFinished?.Invoke(success);
            Dispose();
        }

        private void CheckForDownloadComplete(bool completed)
        {
            if (RenderProjectsReplicator.Completed && RecordedAudioReplicator.Completed)
            {
                _essentialsWrapper.InvokeOnMainThread(async () =>
                {
                    foreach (var projectId in ProjectChannels.Select(projectString => new Guid(projectString)))
                    {
                        var project = await ProjectRepository.GetAsync(projectId);
                        if (project == null)
                        {
                            RaiseDownloadFinished(false);
                            return;
                        }

                        //Query for workflow status if there are none, we did not successfully download the entire project.
                        var workflowStatuses = await WorkflowStatusRepository.QueryOnFieldAsync("ProjectId", project.Id.ToString(), 0);
                        if (workflowStatuses.Any()) continue;

                        RaiseDownloadFinished(false);
                        Dispose();
                        return;
                    }

                    RaiseDownloadFinished(true);
                    Dispose();
                });
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            RenderProjectsReplicator.Dispose();
            RecordedAudioReplicator.Dispose();
            
            DownloadFinished = null;
            _disposed = true;
        }
    }
}