namespace Render.Repositories.Audio
{
    public interface IAudioRepository<T> : IDisposable where T: Models.Audio.Audio
    {
        Task<T> GetByIdAsync(Guid id);
        
        Task<T> GetByParentIdAsync(Guid parentId);
        
        Task<List<T>> GetMultipleByProjectIdAsync(Guid projectId);

        Task<List<T>> GetMultipleByParentIdAsync(Guid parentId);

        Task SaveAsync(T audio);

        Task DeleteAudioByParentIdAsync(Guid parentId);
        
        Task DeleteAudioByIdAsync(Guid id);
        
        Task ResetCreatedFromAudioIds(Guid projectId, List<Guid> deletedStandardQuestions);
    }
}