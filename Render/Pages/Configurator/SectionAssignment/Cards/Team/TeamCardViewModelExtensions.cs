using Render.Pages.Configurator.SectionAssignment.Cards.Section;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Team
{
    public static class TeamCardViewModelExtensions
    {
        /// <summary>
        /// Check if we changed priority between previous and next item.
        /// If so, no need to move section.
        /// </summary>
        public static bool CanMoveSection(this TeamCardViewModel card, SectionCardViewModel sectionToMove, int currentIndex)
        {
            if (card.AssignedSections.Count <= 1)
            {
                return false;
            }

            var previousItemPriority = currentIndex >= 1 ?
                card.AssignedSections[currentIndex - 1].Priority : -1;

            var nextItemPriority = currentIndex < card.AssignedSections.Count - 1 ?
                card.AssignedSections[currentIndex + 1].Priority : sectionToMove.Priority;

            if (previousItemPriority < sectionToMove.Priority && sectionToMove.Priority <= nextItemPriority)
            {
                return false;
            }

            return true;
        }

        public static int GetMoveIndexByPriority(this TeamCardViewModel card, SectionCardViewModel sectionToMove, bool isToHigherPriority)
        {
            var targetMoveIndex = isToHigherPriority ? card.AssignedSections.Count - 1 : 0;
            for (int i = 0; i < card.AssignedSections.Count; i++)
            {
                var currentSection = card.AssignedSections[i];
                if (sectionToMove.Priority < currentSection.Priority)
                {
                    targetMoveIndex = isToHigherPriority ? i - 1 : i;
                    break;
                }
            }

            return targetMoveIndex;
        }

        public static int GetInsertIndexByPriority(this TeamCardViewModel card, SectionCardViewModel insertSection)
        {
            var insertIndex = card.AssignedSections.Count;
            for (int i = 0; i < card.AssignedSections.Count; i++)
            {
                var currentSection = card.AssignedSections[i];
                if (insertSection.Priority < currentSection.Priority)
                {
                    insertIndex = i;
                    break;
                }
            }

            return insertIndex;
        }
    }
}