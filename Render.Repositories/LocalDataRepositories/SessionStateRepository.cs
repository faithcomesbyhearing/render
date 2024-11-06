using Render.Models.LocalOnlyData;
using Render.Repositories.Kernel;

namespace Render.Repositories.LocalDataRepositories
{
    public class SessionStateRepository : ISessionStateRepository
    {
        private readonly IDataPersistence<UserProjectSession> _userProjectSessionPersistence;
        
        public SessionStateRepository(IDataPersistence<UserProjectSession> userProjectSessionPersistence)
        {
            _userProjectSessionPersistence = userProjectSessionPersistence;
        }
        
        public async Task<List<UserProjectSession>> GetUserProjectSessionAsync(Guid userId, Guid projectId)
        {
            var sessions = await _userProjectSessionPersistence.QueryOnFieldsAsync(false, 
                new Tuple<string, object>("UserId", userId),
                new Tuple<string, object>("ProjectId", projectId));

            return sessions;
        }

        public async Task SaveSessionStateAsync(UserProjectSession session)
        {
            await _userProjectSessionPersistence.UpsertAsync(session.Id, session);
        }
        
    }
}