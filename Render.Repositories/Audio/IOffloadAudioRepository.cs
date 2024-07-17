namespace Render.Repositories.Audio;

public interface IOffloadAudioRepository
{
    Task OffloadAudioForProject(Guid projectId);
}