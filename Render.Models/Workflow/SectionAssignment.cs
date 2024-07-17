using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow
{
    public class SectionAssignment : ValueObject
    {
        public Guid SectionId { get; }
        public int Priority { get; set; }

        public SectionAssignment(Guid sectionId, int priority)
        {
            SectionId = sectionId;
            Priority = priority;
        }

        public SectionAssignment IncreasePriorityBy1()
        {
            return new SectionAssignment(SectionId, Priority + 1);
        }

        public SectionAssignment DecreasePriorityBy1()
        {
            return new SectionAssignment(SectionId, Priority - 1);
        }
    }
}