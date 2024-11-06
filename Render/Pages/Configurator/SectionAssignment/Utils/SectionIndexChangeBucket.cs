using Render.Pages.Configurator.SectionAssignment.Cards.Section;

namespace Render.Pages.Configurator.SectionAssignment.Utils;

public record SectionIndexChangeBucket
{
    public SectionCardViewModel Section { get; }

    public int OldIndex { get; }

    public int NewIndex { get; set; }

    public SectionIndexChangeBucket(SectionCardViewModel section, int oldIndex = -1, int newIndex = -1)
    {
        Section = section;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }
}