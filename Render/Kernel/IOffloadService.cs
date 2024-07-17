namespace Render.Kernel
{
    public interface IOffloadService
    {
        Task OffloadProject(Guid projectId);
        Task OffloadFailedProjects();
        Task OffloadIncompleteProjects();
        Task<string> GetOffloadProjectSize(Guid projectId);
    }
}