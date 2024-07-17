using System.Reactive;

namespace Render.Kernel;

public interface IProjectDownloadService 
{
    void StartDownload(Guid projectId, List<Guid> globalUserIds, string syncGatewayUsername, string syncGatewayPassword);
    void ResumeDownload(Guid projectId, List<Guid> globalUserIds, string syncGatewayUsername, string syncGatewayPassword);
    void StopDownload(Guid projectId);
    IObservable<bool> WhenDownloadFinished(Guid projectId);
}