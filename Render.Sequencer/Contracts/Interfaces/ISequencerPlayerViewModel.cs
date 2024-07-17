using Render.Sequencer.Contracts.Models;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerPlayerViewModel : ISequencerViewModel
{
    AudioModel? GetCurrentAudio();

    void SetAudio(PlayerAudioModel[] audios);

    void HasTimer(bool state);
}