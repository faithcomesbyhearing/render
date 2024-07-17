using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Sections;

namespace Render.Pages.Configurator.SectionAssignment
{
    public class TeamSectionAssignment : ReactiveObject
    {
        [Reactive]
        public TeamTranslatorUser Team { get; set; }
        
        [Reactive]
        public Section Section { get; set; }
        
        [Reactive]
        public int Priority { get; set; }

        public TeamSectionAssignment(Section section, TeamTranslatorUser team, int priority)
        {
            Team = team;
            Section = section;
            Priority = priority;
        }
    }
}