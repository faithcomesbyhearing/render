using ReactiveUI;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.Models.Combining;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerCombiningPlayerViewModel : ISequencerViewModel
{
    ReactiveCommand<AudioModel, bool>? TryUnlockAudioCommand { get; set; }

    void SetAudio(CombinableAudioModel[] audios);

    CombinedResultModel GetCombinedResult();
}