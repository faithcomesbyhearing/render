using Render.Repositories.Kernel;

namespace Render.Repositories.Audio
{
    public class AudioRepository<T> : IAudioRepository<T> where T : Models.Audio.Audio
    {
        private readonly IDataPersistence<T> _audioRepository;

        private SemaphoreSlim SemaphoreSlim { get; }

        public AudioRepository(IDataPersistence<T> audioRepository = null)
        {
            _audioRepository = audioRepository;
            SemaphoreSlim = new SemaphoreSlim(1);
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            return await _audioRepository.GetAsync(id);
        }

        public virtual async Task<T> GetByParentIdAsync(Guid parentId)
        {
            return await _audioRepository.QueryOnFieldAsync(nameof(Models.Audio.Audio.ParentId), parentId.ToString());
        }

        public virtual async Task<List<T>> GetMultipleByProjectIdAsync(Guid projectId)
        {
            return await _audioRepository.QueryOnFieldAsync(nameof(Models.Audio.Audio.ProjectId), projectId.ToString(), 0);
        }

        public virtual async Task<List<T>> GetMultipleByParentIdAsync(Guid parentId)
        {
            return await _audioRepository.QueryOnFieldAsync(
                searchField: nameof(Models.Audio.Audio.ParentId), 
                value: parentId.ToString(), 
                limit: 0);
        }

        public virtual async Task SaveAsync(T audio)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                await _audioRepository.UpsertAsync(audio.Id, audio);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public virtual async Task<bool> CheckForAudioAsync(Guid parentId)
        {
            var audio = await GetByIdAsync(parentId);
            return audio != null;
        }

        public virtual async Task DeleteAudioByParentIdAsync(Guid parentId)
        {
            var audio = await GetByParentIdAsync(parentId);
            await DeleteAllRelatedDocumentsAsync(audio);
        }

        public virtual async Task DeleteAudioByIdAsync(Guid id)
        {
            var audio = await _audioRepository.GetAsync(id);
            await DeleteAllRelatedDocumentsAsync(audio);
        }

        protected virtual async Task DeleteAllRelatedDocumentsAsync(T audio)
        {
            if (audio != null)
            {
                await _audioRepository.DeleteAsync(audio.Id);
            }
        }
        
        public async Task Purge(Guid id)
        {
            await _audioRepository.PurgeAllOfTypeForProjectId(id);
        }

        // Used when the StandardQuestion (Library Question) is deleted.
        // Resets the CreatedFromAudioId property for all NotableAudio entities that reference the specified standard questions.
        public async Task ResetCreatedFromAudioIds(Guid projectId, List<Guid> deletedStandardQuestions)
        {
            await _audioRepository.ResetCreatedFromAudioIds(projectId, deletedStandardQuestions);
        }

        public virtual void Dispose()
        {
            _audioRepository?.Dispose();
        }
    }
}