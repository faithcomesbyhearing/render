using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class MachineLoginState : DomainEntity
    {
        [JsonProperty("TopFourUserIds")]
        private List<UserLogin> TopFourUserIds { get; set;} = new List<UserLogin>();
        
        [JsonProperty("LastProjectId")]
        public Guid LastProjectId { get; private set; }

        public MachineLoginState() : base(Version)
        {
            DomainEntityVersion = Version;
        }

        public void SetProjectLogin(Guid projectId)
        {
            LastProjectId = projectId;
        }

        public void AddUserLogIn(Guid userId)
        {
            if (TopFourUserIds.Select(x => x.UserId).Contains(userId))
            {
                var userLogin = TopFourUserIds.First(x => x.UserId == userId);
                userLogin.LastLogIn = DateTimeOffset.Now;
            }

            if (TopFourUserIds.Count >= 4)
            {
                TopFourUserIds = TopFourUserIds.OrderBy(x => x.LastLogIn).Skip(1).Take(3).ToList();
            }
            TopFourUserIds.Add(new UserLogin(userId, DateTimeOffset.Now));
        }

        public void RemoveUser(Guid userId)
        {
            if (TopFourUserIds.Select(x => x.UserId).Contains(userId))
            { 
                TopFourUserIds.RemoveAll(x => x.UserId == userId);
            }
        }

        public List<Guid> GetTopFourUserIds()
        {
            return TopFourUserIds.Select(x => x.UserId).ToList();
        }
        
        private const int Version = 1;

        private class UserLogin
        {
            [JsonProperty("UserId")]
            public Guid UserId { get; set; }
            
            [JsonProperty("LastLogIn")]
            public DateTimeOffset LastLogIn { get; set; }

            public UserLogin(Guid userId, DateTimeOffset lastLogIn)
            {
                UserId = userId;
                LastLogIn = lastLogIn;
            }
        }
    }
}