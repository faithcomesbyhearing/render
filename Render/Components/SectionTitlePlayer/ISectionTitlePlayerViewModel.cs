using ReactiveUI;
using System.Reactive;

namespace Render.Components.SectionTitlePlayer
{
    public interface ISectionTitlePlayerViewModel : IDisposable
    {
        string SectionNumber { get; }

        string PassageNumber { get; }

        bool HasAudio { get; }

        bool IsPlaying { get; }

        SectionTitlePlayerState SectionTitlePlayerState { get; }

        ReactiveCommand<Unit, Unit> ButtonClickCommand { get; }

        void ButtonClick();

        void Pause();
    }
}