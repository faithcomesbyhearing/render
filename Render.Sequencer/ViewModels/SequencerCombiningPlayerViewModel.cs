using System.Reactive.Linq;
using ReactiveUI;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Contracts.Models.Combining;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Views.WaveForm;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Sequencer.ViewModels;

internal class SequencerCombiningPlayerViewModel : BaseSequencerViewModel<CombiningWaveFormViewModel, CombiningScrollerViewModel>, ISequencerCombiningPlayerViewModel
{
    private WavHelper.WavMergeResult? _resultToDelete;

    public ReactiveCommand<AudioModel, bool>? TryUnlockAudioCommand 
    { 
        get => Sequencer.TryUnlockAudioCommand;
        set => Sequencer.TryUnlockAudioCommand = value; 
    }

    internal SequencerCombiningPlayerViewModel(Func<IAudioPlayer> playerFactory)
        : base(SequencerMode.Player, playerFactory, null, FlagType.None, isCombining: true)
    {
        WaveFormViewModel = new CombiningWaveFormViewModel(Sequencer);
        ScrollerViewModel = new CombiningScrollerViewModel(Sequencer);
    }

    public void SetAudio(CombinableAudioModel[] audios)
    {
        if (audios.IsEmpty())
        {
            throw new ArgumentException(ErrorMessages.AudiosCantBeEmpty);
        }

        Sequencer.SetCombinedAudios(audios);
    }

    public CombinedResultModel GetCombinedResult()
    {
        if (_resultToDelete is not null)
        {
            throw new InvalidOperationException(ErrorMessages.CombiningMoreThanOnes);
        }

        var combiningItem = WaveFormViewModel?.CombiningItem ?? throw new InvalidOperationException(ErrorMessages.NullCombiningItem);
        var baseAudio = combiningItem.CombinableWaveFormItems.First(item => item.SequencerAudio.Audio.IsBase).SequencerAudio.Audio;

        return new CombinedResultModel(
            startTime: combiningItem.CombinableWaveFormItems.First().SequencerAudio.StartPosition,
            endTime: combiningItem.CombinableWaveFormItems.Last().SequencerAudio.EndPosition,
            combinedAudiosIds: combiningItem.CombinableWaveFormItems.Select(item => item.SequencerAudio.Audio.Key).ToArray());
    }

    public override void Dispose()
    {
        base.Dispose();

        _resultToDelete?.DeleteSourceFiles();
        _resultToDelete = null;
    }
}