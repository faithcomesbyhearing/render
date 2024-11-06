namespace Render.Kernel
{
    public interface IOffloadService
    {
        Task OffloadProject(Guid projectId);
        Task OffloadProjectsData(List<Guid> projectsIds);
        Task OffloadFailedProjects();
        Task<string> GetOffloadProjectSize(Guid projectId);
    }
}