using Render.Pages.Configurator.SectionAssignment.Cards.Section;

namespace Render.Pages.Configurator.SectionAssignment.Manager;

public static class SectionCollectionsManagerExtensions
{
    public static bool CanMoveSection(this SectionCollectionsManager _,
        SectionCardViewModel sectionToMove,
        SectionCardViewModel anchorSection)
    {
        if (sectionToMove?.AssignedTeamViewModel is null ||
            sectionToMove.AssignedTeamViewModel.AssignedSections.Count <= 1 ||
            sectionToMove == anchorSection)
        {
            return false;
        }

        return true;
    }

    public static int GetInsertIndexByNumber(this SectionCollectionsManager manager, SectionCardViewModel section)
    {
        var insertIndex = manager.UnassignedSectionCards.Count;
        for (int i = 0; i < manager.UnassignedSectionCards.Count; i++)
        {
            var currentSection = manager.UnassignedSectionCards[i];
            if (section.Number < currentSection.Number)
            {
                insertIndex = i;
                break;
            }
        }

        return insertIndex;
    }
}