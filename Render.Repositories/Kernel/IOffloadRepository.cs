namespace Render.Repositories.Kernel;

public interface IOffloadRepository
{
    Task OffloadAsync(Guid projectId);

    Task PurgeRenderLocalOnlyData();
}