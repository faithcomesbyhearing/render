using System.Collections.Concurrent;
using System.Reactive.Linq;
using Render.Services.SyncService;

namespace Render.Kernel;

public class ProjectDownloadService : IProjectDownloadService
{
    private ConcurrentDictionary<Guid, IOneShotReplicator> Replicators { get; } = new();

    private readonly IViewModelContextProvider _viewModelContextProvider;

    public ProjectDownloadService(IViewModelContextProvider viewModelContextProvider)
    {
        _viewModelContextProvider = viewModelContextProvider;
    }

    public void StartDownload(Guid projectId, List<Guid> globalUserIds, string syncGatewayUsername, string syncGatewayPassword)
    {
        var replicator = InitializeProjectDownloader(projectId, globalUserIds, syncGatewayUsername, syncGatewayPassword);
        replicator?.StartDownload(true);
    }

    public void ResumeDownload(Guid projectId, List<Guid> globalUserIds, string syncGatewayUsername, string syncGatewayPassword)
    {
        var replicator = InitializeProjectDownloader(projectId, globalUserIds, syncGatewayUsername, syncGatewayPassword);
        replicator?.StartDownload();
    }

    public void StopDownload(Guid projectId)
    {
        var replicator = GetReplicator(projectId);
        replicator?.StopDownload();
        RemoveReplicator(projectId);
    }

    public IObservable<bool> WhenDownloadFinished(Guid projectId)
    {
        var replicator = GetReplicator(projectId);
        if (replicator == null) return Observable.Return(false);

        return Observable.FromEvent<Action<bool>, bool>(
            handler => replicator.DownloadFinished += handler,
            handler => replicator.DownloadFinished -= handler);
    }

    private IOneShotReplicator InitializeProjectDownloader(Guid projectId, List<Guid> globalUserIds, string syncGatewayUsername, string syncGatewayPassword)
    {
        if (Replicators.ContainsKey(projectId)) return GetReplicator(projectId);

        var replicator = new OneShotReplicatorRetryProxy(
            _viewModelContextProvider.GetLogger(typeof(WebSyncService)),
            projectId,
            () => _viewModelContextProvider.GetOneShotReplicator(
                new List<Guid> { projectId },
                globalUserIds,
                syncGatewayUsername,
                syncGatewayPassword)
        );

        Replicators.GetOrAdd(projectId, replicator);

        replicator.DownloadFinished += _ => RemoveReplicator(projectId);

        return replicator;
    }

    private IOneShotReplicator GetReplicator(Guid projectId)
    {
        return Replicators.GetValueOrDefault(projectId);
    }

    private void RemoveReplicator(Guid projectId)
    {
        var replicator = GetReplicator(projectId);
        if (replicator is null) return;

        replicator.Dispose();
        Replicators.TryRemove(projectId, out replicator);
    }
}