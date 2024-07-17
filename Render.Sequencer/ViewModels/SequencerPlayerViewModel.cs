using System.Reactive.Linq;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Scroller;
using Render.Sequencer.Views.Toolbar;
using Render.Sequencer.Views.WaveForm;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Sequencer.ViewModels;

internal class SequencerPlayerViewModel : BaseSequencerViewModel<WaveFormViewModel, ScrollerViewModel>, ISequencerPlayerViewModel
{
    internal SequencerPlayerViewModel(Func<IAudioPlayer> playerFactory, FlagType flagType)
        : base(SequencerMode.Player, playerFactory, null, flagType)
    {
        WaveFormViewModel = new WaveFormViewModel(Sequencer, flagType);
        ScrollerViewModel = new ScrollerViewModel(Sequencer);
    }

    public void SetAudio(PlayerAudioModel[] audios)
    {
        if (audios.IsEmpty())
        {
            throw new ArgumentException(ErrorMessages.AudiosCantBeEmpty);
        }

        Sequencer.SetAudios(audios);
    }

    public AudioModel? GetCurrentAudio()
    {
        return Sequencer?.CurrentAudio?.Audio;
    }

    public void HasTimer(bool state)
    {
        ToolbarViewModel.HasTimer = state;
    }
}