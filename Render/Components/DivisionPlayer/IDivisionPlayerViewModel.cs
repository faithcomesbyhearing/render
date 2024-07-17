using Render.Components.BarPlayer;
using Render.Models.Sections;

namespace Render.Components.DivisionPlayer
{
    public interface IDivisionPlayerViewModel: IBarPlayerViewModel
    {
        SectionReferenceAudio ReferenceAudio { get; }
        bool IsLocked { get; }
        int DivisionsCount { get; }
        void ApplyChanges();
    }
}