using System.Reflection;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces;
using Render.Interfaces.EssentialsWrappers;
using Render.Models.Sections;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;
using Render.TempFromVessel.Project;

namespace Render.Services.SyncService
{
    public class LocalProjectDownloader : ReactiveObject, ILocalProjectDownloader, IDisposable
    {
        private readonly IEssentialsWrapper _essentialsWrapper;
        private readonly List<IDisposable> _disposables = new ();

        private ILocalReplicator RenderProjectsReplicator { get; set; }
        private ILocalReplicator RenderAdminReplicator { get; set; }
        private ILocalReplicator RecordedAudioReplicator { get; set; }
        private IDataPersistence<Project> ProjectRepository { get; }
        private IDataPersistence<WorkflowStatus> WorkflowStatusRepository { get; }

        private string Channel { get; }
        private Guid ProjectId { get; }
        private Device Device { get; set; }

        [Reactive] private bool FirstWaveReplicationDone { get; set; }

        public event Action<LocalReplicationResult> DownloadFinished;

        public LocalProjectDownloader(
            IAppSettings appSettings,
            Guid projectId, IRenderLogger renderLogger,
            IDataPersistence<Project> projectRepository,
            IDataPersistence<WorkflowStatus> workflowStatusRepository,
            IEssentialsWrapper essentialsWrapper,
            string localFolderPath)
        {
            _essentialsWrapper = essentialsWrapper;

            ProjectId = projectId;
            Channel = projectId.ToString();
            ProjectRepository = projectRepository;
            WorkflowStatusRepository = workflowStatusRepository;

            //Setup first wave of replicators
            RenderProjectsReplicator = new LocalReplicator(renderLogger, localFolderPath);
            RenderProjectsReplicator.Configure(
                databaseName: Buckets.render.ToString(),
                portNumber: appSettings.CouchbaseStartingPort,
                peerUsername: appSettings.CouchbasePeerUsername,
                peerPassword: appSettings.CouchbasePeerPassword);

            RecordedAudioReplicator = new LocalReplicator(renderLogger, localFolderPath);
            RecordedAudioReplicator.Configure(
                databaseName: Buckets.renderaudio.ToString(),
                portNumber: appSettings.CouchbaseStartingPort + 3,
                peerUsername: appSettings.CouchbasePeerUsername,
                peerPassword: appSettings.CouchbasePeerPassword);

            _disposables.Add(this
                .WhenAnyValue(x => x.RenderProjectsReplicator.Result, x => x.RecordedAudioReplicator.Result)
                .Subscribe(x => CheckForDownloadComplete(x.Item1, x.Item2)));

            //Setup second wave of replicators that have to wait for the first wave to finish
            RenderAdminReplicator = new LocalReplicator(renderLogger, localFolderPath);
            RenderAdminReplicator.Configure(
                databaseName: Buckets.render.ToString(),
                portNumber: appSettings.CouchbaseStartingPort + 1,
                peerUsername: appSettings.CouchbasePeerUsername,
                peerPassword: appSettings.CouchbasePeerPassword);
            
            _disposables.Add(this
                .WhenAnyValue(x => x.RenderAdminReplicator.Result)
                .Subscribe(CheckForSecondDownloadComplete));
        }

        private void CheckForDownloadComplete(LocalReplicationResult projectRepResult, LocalReplicationResult audioRepResult)
        {
            if (projectRepResult == LocalReplicationResult.NotYetComplete &&
                audioRepResult == LocalReplicationResult.NotYetComplete)
            {
                return;
            }

            if (projectRepResult == LocalReplicationResult.Succeeded && audioRepResult == LocalReplicationResult.Succeeded)
            {
                FirstWaveReplicationDone = true;

                _essentialsWrapper.InvokeOnMainThread(async () =>
                {
                    var project = await ProjectRepository.GetAsync(ProjectId);
                    if (project == null)
                    {
                        FinishDownload(LocalReplicationResult.ProjectNotFound);
                    }
                    else
                    {
                        //Query for workflow status if there are none, we did not successfully download the entire project.
                        var workflowStatuses = await WorkflowStatusRepository.QueryOnFieldAsync("ProjectId",
                            project.Id.ToString(), 0);
                        if (!workflowStatuses.Any())
                        {
                            FinishDownload(LocalReplicationResult.ProjectNotFound);
                        }
                        else
                        {
                            RenderAdminReplicator.AddReplicator(
                            device: Device,
                            ProjectId,
                            filterIds: project.GlobalUserIds.Select(x => x.ToString()).ToList(),
                            oneShotDownload: true,
                            freshDownload: true);
                        }
                        
                    }
                });
            }
            else if (projectRepResult == LocalReplicationResult.Failed || audioRepResult == LocalReplicationResult.Failed)
            {
                FinishDownload(LocalReplicationResult.Failed);
            }
        }
        
        private void CheckForSecondDownloadComplete(LocalReplicationResult result)
        {
            //Check render projects replication and recorded audio replication is done before 
            if (FirstWaveReplicationDone)
            {
                FinishDownload(result == LocalReplicationResult.Succeeded ? 
                    LocalReplicationResult.Succeeded : 
                    LocalReplicationResult.Failed);
            }
        }
        
        public bool BeginActiveLocalReplicationOfProject(Device device)
        {
            try
            {
                Device = device;
                device.IsConnected = true;
                RenderProjectsReplicator.AddReplicator(device,ProjectId, new List<string> {Channel}, oneShotDownload: true, freshDownload: true);
                RecordedAudioReplicator.AddReplicator(device, ProjectId, new List<string> {Channel}, oneShotDownload: true, freshDownload: true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void FinishDownload(LocalReplicationResult result)
        {
            Device.IsConnected = false;
            DownloadFinished?.Invoke(result);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            RenderProjectsReplicator?.Dispose();
            RenderProjectsReplicator = null;
            RecordedAudioReplicator?.Dispose();
            RenderProjectsReplicator = null;
            RenderAdminReplicator?.Dispose();
            RenderAdminReplicator = null;
        }
    }
}