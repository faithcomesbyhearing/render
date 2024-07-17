namespace Render.Components.AudioRecorder;

public enum AudioRecorderState
{
    NoAudio,
    Recording,
    CanPlayAudio,
    PlayingAudio,
    // temporary deleted state is not the same as no audio
    // no audio is a new draft and temporary deleted audio is an audio wit data which can be recovered
    TemporaryDeleted,
    CanAppendAudio
}