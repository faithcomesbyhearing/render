namespace Render.Sequencer.Core.Utils.Errors;

internal static class ErrorMessages
{
    internal const string AudioIsPlaying = "Audio is playing.";
    internal const string AudioIsNull = "Audio can't be null here.";
    internal const string AudioIsNotPlaying = "Audio is not playing.";
    internal const string ReadingRecordError = "Error occurred during reading recorded audio file from local storage.";
    internal const string AudioToSelectCantBeNull = "AudioToSelect can't be null here. Check Audios count and CurrentPosition.";
    internal const string RecordCreatingWhileExists = "Creating new record while previous is still exist.";
    internal const string InvalidFlagType = "Flag type is not supported.";
    internal const string InvalidWaveFormItemType = "WaveFormItem type is not supported.";
    internal const string RecorderIsNotInitialized = "Recorder is not initialized.";
    internal const string RecorderFactoryMustReturnNotNullObject = "Recorder factory must return not null object.";
    internal const string AudiosCantBeEmpty = "AudioModels array must contain at least one item.";
    internal const string OneBaseAudioAllowed = "In audio combining state only one base audio allowed.";
    internal const string BaseAudioMustBeSet = "In audio combining state single base audio must be set.";
    internal const string NullCombiningItem = "CombiningWaveWorkItemViewModel can't be null here.";
    internal const string CombiningMoreThanOnes = "You must combine audio only ones.";
}