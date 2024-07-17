using Render.Models.Users;
using Render.Models.Workflow;

namespace Render.Pages.Configurator.SectionAssignment
{
    public class TeamTranslatorUser
    {
        public Team Team { get; private set; }
        public IUser User { get; private set; }

        public TeamTranslatorUser(Team team, IUser user)
        {
            Team = team;
            User = user;
        }
    }
}