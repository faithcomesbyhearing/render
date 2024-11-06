using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;

namespace Render.Pages.Configurator.SectionAssignment.Factory;

public record SectionAssignmentCards(
    IList<TeamCardViewModel> TeamCards,
    IList<SectionCardViewModel> SectionCards)
{
    public static SectionAssignmentCards Empty
    {
        get => new([], []);
    }
};