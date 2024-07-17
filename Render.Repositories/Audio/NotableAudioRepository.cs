using Render.Models.Audio;
using Render.Repositories.Kernel;

namespace Render.Repositories.Audio
{
    public class NotableAudioRepository<T> : AudioRepository<T> where T: NotableAudio
    {
        private readonly IAudioRepository<Models.Audio.Audio> _conversationRepository;

        public NotableAudioRepository(IDataPersistence<T> audioRepository, IAudioRepository<Models.Audio.Audio> conversationRepository)
        : base(audioRepository)
        {
            _conversationRepository = conversationRepository;
        }
        
        public override async Task<T> GetByIdAsync(Guid id)
        {
            var audio = await base.GetByIdAsync(id);
            if(audio == null) return null;

            audio = await GetAllConversationMediaAudio(audio);
            return audio;
        }

        public override async Task<T> GetByParentIdAsync(Guid parentId)
        {
            var audio = await base.GetByParentIdAsync(parentId);
            if (audio == null) return null;

            audio = await GetAllConversationMediaAudio(audio);
            return audio;
        }

        public override async Task<List<T>> GetMultipleByProjectIdAsync(Guid projectId)
        {
            var audioList = new List<T>();
            var audios = await base.GetMultipleByProjectIdAsync(projectId);
            foreach (var notableAudio in audios)
            {
                var notableAudioWithMedia
                    = await GetAllConversationMediaAudio(notableAudio);
                audioList.Add(notableAudioWithMedia);
            }
            return audioList;
        }

        public override async Task<List<T>> GetMultipleByParentIdAsync(Guid parentId)
        {
            var audioList = new List<T>();
            var audios = await base.GetMultipleByParentIdAsync(parentId);

            foreach (var notableAudio in audios)
            {
                var notableAudioWithMedia 
                    = await GetAllConversationMediaAudio(notableAudio);
                audioList.Add(notableAudioWithMedia);
            }

            return audioList;
        }

        protected override async Task DeleteAllRelatedDocumentsAsync(T audio)
        {
            if (audio != null)
            {
                foreach (var message in audio.Conversations.SelectMany(x => x.Messages))
                {
                    await _conversationRepository.DeleteAudioByIdAsync(message.Media.AudioId);
                }
                await base.DeleteAllRelatedDocumentsAsync(audio);
            }
        }
        
        private async Task<T> GetAllConversationMediaAudio(T audio)
        {
            foreach (var conversation in audio.Conversations)
            {
                foreach (var message in conversation.Messages)
                {
                    message.Media.Audio = await _conversationRepository.GetByIdAsync(message.Media.AudioId);
                    //Assuming that if we have an interpretation, we always want to get it
                    message.InterpretationAudio = await _conversationRepository.GetByParentIdAsync(message.Id);
                }
            }
            return audio;
        }

        public override void Dispose()
        {
            _conversationRepository?.Dispose();

            base.Dispose();
        }
    }
}