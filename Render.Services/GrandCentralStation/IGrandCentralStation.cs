
namespace Render.Services.GrandCentralStation
{
    public interface IGrandCentralStation : IDisposable
    {
        Guid CurrentProjectId { get; }
        
        Task FindWorkForUser(Guid projectId, Guid userId);
        
        void ResetWorkForUser();
    }
}