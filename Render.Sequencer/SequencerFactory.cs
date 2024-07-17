using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.ViewModels;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Sequencer;

public class SequencerFactory : ISequencerFactory
{
    public ISequencerPlayerViewModel CreatePlayer(
        Func<IAudioPlayer> playerFactory,
        FlagType flagType = FlagType.None)
    {
        return new SequencerPlayerViewModel(playerFactory, flagType);
    }

    public ISequencerCombiningPlayerViewModel CreateCombiningPlayer(
        Func<IAudioPlayer> playerFactory)
    {
        return new SequencerCombiningPlayerViewModel(playerFactory);
    }

    public ISequencerRecorderViewModel CreateRecorder(
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder> recorderFactory,
        FlagType flagType = FlagType.None)
    {
        return new SequencerRecorderViewModel(playerFactory, recorderFactory, flagType);
    }

    public ISequencerEditorViewModel CreateEditor(
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder> recorderFactory)
    {
        return new SequencerEditorViewModel(playerFactory, recorderFactory);
    }
}