namespace Render.Repositories.LocalDataRepositories
{
    public class LocalRenderChangeMonitoringService : LocalChangeMonitoringService, IRenderChangeMonitoringService
    {
        public LocalRenderChangeMonitoringService(string databasePath) : base(bucket: "render", databasePath)
        {
            
        }
    }
}