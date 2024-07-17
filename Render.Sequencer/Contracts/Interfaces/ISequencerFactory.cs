using Render.Sequencer.Contracts.Enums;
using Render.Services.AudioPlugins.AudioPlayer;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerFactory
{
    public ISequencerPlayerViewModel CreatePlayer(
        Func<IAudioPlayer> playerFactory,
        FlagType flagType = FlagType.None);

    public ISequencerCombiningPlayerViewModel CreateCombiningPlayer(
        Func<IAudioPlayer> playerFactory);

    public ISequencerRecorderViewModel CreateRecorder(
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder> recorderFactory,
        FlagType flagType = FlagType.None);

    public ISequencerEditorViewModel CreateEditor(
        Func<IAudioPlayer> playerFactory,
        Func<IAudioRecorder> recorderFactory);
}