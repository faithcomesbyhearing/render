using Render.Models.Sections;

namespace Render.Services.AudioServices
{
    public interface IBreathPauseAnalyzer
    {
        List<int> Division { get; }
        List<TimeMarkers> BreathPauseSegments { get; }
        bool IsAudioLoaded { get; }
        void LoadAudioAndFindBreathPauses(ITempAudioService tempAudioService);
        void RemoveLastExtraDivision();
	}
}