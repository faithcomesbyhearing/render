using System.Reactive;
using ReactiveUI;
using Render.Kernel;
using Render.Models.Sections;
using Render.Services.AudioServices;

namespace Render.Components.BarPlayer
{
    public interface IBarPlayerViewModel : IActionViewModelBase
    {
        string Glyph { get; set; }
        string AudioTitle { get; set; }

        bool ShowPlayButton { get; }
        bool ShowPauseButton { get; }
        bool Loading { get; set; }
        bool PaintPassageMarkers { get; }
        bool ShowSecondaryButton { get; }

        Double CurrentPosition { get; set; }
        Double Duration { get; }
        int PlayerPositionInList { get; }
        float [] AudioSamples { get; set; }

        BarPlayerPassagePainter PassagePainter { get; }
        ImageSource SecondaryButtonIcon { get;  }
        Color SecondaryButtonBackgroundColor { get; set; }

		Color GlyphColor { get; }
		
		AudioPlayerState AudioPlayerState { get; set; }

        ReactiveCommand<Unit, IRoutableViewModel> SecondaryButtonClickCommand { get; }
        ReactiveCommand<Unit, Unit> PlayAudioCommand { get; }
        ReactiveCommand<Unit, Unit> PauseAudioCommand { get; }
        ReactiveCommand<Unit, Unit> SeekCommand { get; }
        ReactiveCommand<Unit, Unit> PauseOnSeekCommand { get; }

        void Seek(double position);

        void Pause();

        void SetPassages(List<TimeMarkers> passageMarkers);

        void SetSecondaryButtonBackgroundColor(Color color);

		void SetGlyphColor(Color color);
	}
}