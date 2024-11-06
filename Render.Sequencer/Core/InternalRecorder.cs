using System.Reactive.Linq;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Streams;
using Render.Services.AudioPlugins.AudioRecorder.Interfaces;
using static Render.Sequencer.Core.Utils.Helpers.WavHelper;

namespace Render.Sequencer.Core;

public class InternalRecorder : IDisposable
{
    public const string DefaultRecordName = "Record 1";
    public const double DefaultTimerIntervalMs = 100;
    public const int SecondsOnScreen = 30;

    private InternalSequencer _sequencer;
    private List<IDisposable> _disposables;
    private ObservableRecordFileStream? _recorderStream;
    private Func<IAudioRecorder>? _recorderFactory;

    private IAudioRecorder? _recorder;
    internal IAudioRecorder? Recorder
    {
        get => _recorder;
    }

    public AudioDetails AudioDetails
    {
        get => Recorder?.AudioStreamDetails ?? new AudioDetails
        {
            BitsPerSample = 16,
            ChannelCount = 1,
            SampleRate = 48000,
        };
    }

    public InternalRecorder(InternalSequencer sequencer, Func<IAudioRecorder>? recorderFactory)
    {
        _disposables = new();
        _sequencer = sequencer;
        _recorderFactory = recorderFactory;
    }

    internal async Task StartAsync()
    {
        var allowRecord = await _sequencer.HasRecordPermissionCommand.SafeExecute();
        if (allowRecord is false)
        {
            return;
        }

        _recorder = CreateRecorder();
        _recorderStream = CreateRecorderStream(_recorder);
        
        if (_sequencer.AppendRecordMode is false)
        {
            _sequencer.Flags.Clear();
            _sequencer.CurrentAudio.RemoveAudioData();
        }

        _sequencer.LastDeletedAudio = null;
        _sequencer.Mode = SequencerMode.Recorder;
        _sequencer.State = SequencerState.Recording;

        await _recorder.StartRecording(_recorderStream);
        await _sequencer.OnRecordingStartedCommand.SafeExecute();
    }

    internal async Task StopAsync()
    {
        if (_recorder is null)
        {
            throw new NullReferenceException(ErrorMessages.RecorderIsNotInitialized);
        }

        if (_sequencer.IsNotRecording())
        {
            return;
        }

        var path = await _recorder.StopRecording();
        ResetRecorder();

        var isSuccess = TryLoadAudio(path);

        _sequencer.AppendRecordMode = false;
        _sequencer.Mode = isSuccess ? SequencerMode.Player : SequencerMode.Recorder;
        _sequencer.State = SequencerState.Loaded;

        _sequencer.ForceScrubberToStart();
        _sequencer.ForceScrollerToStart();

        var command = isSuccess ?
            _sequencer.OnRecordingFinishedCommand :
            _sequencer.OnEmptyRecordingFinishedCommand;

        await command.SafeExecute();
    }

    internal async Task MakeRecordTempAsync()
    {
        _sequencer.CurrentPosition = default;
        _sequencer.TotalCurrentPosition = default;
        _sequencer.CurrentAudio.MakeTemp();

        _sequencer.Mode = SequencerMode.Recorder;
        _sequencer.State = SequencerState.Loaded;

        await _sequencer.OnDeleteRecordCommand.SafeExecute();
    }

    internal async Task RevertTempRecordAsync()
    {
        _sequencer.State = SequencerState.Loaded;
        _sequencer.Mode = SequencerMode.Player;

        _sequencer.CurrentAudio.RefreshPlayerSamples(_sequencer.GetPlayerBuildSamplesParams(_sequencer.CurrentAudio.Duration));
        _sequencer.CurrentAudio.MakeAvailable();

        await _sequencer.OnUndoDeleteRecordCommand.SafeExecute();
    }

    private IAudioRecorder CreateRecorder()
    {
        ResetRecorder();

        var recorder = _recorderFactory?.Invoke();
        if (recorder is null)
        {
            return _recorder ?? throw new InvalidOperationException(ErrorMessages.RecorderFactoryMustReturnNotNullObject);
        }

        recorder.OnRecordFailed += OnRecordFailed;
        recorder.OnRecordDeviceRestore += OnRecordDeviceRestore;

        return recorder;
    }

    private ObservableRecordFileStream CreateRecorderStream(IAudioRecorder recorder)
    {
        _recorderStream = new ObservableRecordFileStream(
            path: recorder.GetAudioFilePath(), 
            audioDetails: recorder.AudioStreamDetails, 
            chunkDurationMs: _sequencer.GetFineTunedRecorderTimerInterval());

        recorder.OnRecordFinished += _recorderStream.RecordFinished;
        _recorderStream.AudioDataRecorded += AudioDataRecorded;

        return _recorderStream;
    }

    private async void OnRecordFailed(object? sender, string deviceId)
    {
        await _sequencer.OnRecordFailedCommand.SafeExecute();
    }

    private async void OnRecordDeviceRestore(object? sender, string deviceId)
    {
        await _sequencer.OnRecordDeviceRestoreCommand.SafeExecute();
    }

    private bool TryLoadAudio(string audioFilePath)
    {
        String? mergedFilePath = null;
        WavMergeResult? mergeResult = null;

        if (_sequencer.AppendRecordMode && _sequencer.CurrentAudio.HasAudioData())
        {
            var originalAudioFilePath = _sequencer.CurrentAudio.Audio.Path;
            mergeResult = MergeWavFiles(originalAudioFilePath!, audioFilePath);
            mergedFilePath = mergeResult?.MergedFilePath;
        }

        var isSuccess = _sequencer.CurrentAudio.TrySetAudio(
            startPosition: _sequencer.CurrentAudio.StartPosition,
            audio: RecordAudioModel.Create(
                path: mergedFilePath ?? audioFilePath, 
                name: DefaultRecordName, 
                flags: _sequencer.CurrentAudio.Audio.Flags));

        if (isSuccess)
        {
            mergeResult?.DeleteSourceFiles();
        }

        return isSuccess;
    }
    
    private void AudioDataRecorded(RecordedData data)
    {
        _sequencer.CurrentAudio.SetDuration(data.TotalDurationSec);
        _sequencer.CurrentPosition = _sequencer.TotalDuration;
        _sequencer.TotalCurrentPosition = _sequencer.TotalDuration;

        _sequencer.CurrentAudio.RefreshRecorderSamples(
            dataChunk: data.AudioData,
            buildParams: _sequencer.GetRecordingBuildSamplesParams(
                totalDurationSec: _sequencer.CurrentAudio.Duration));
    }

    private void ResetRecorder()
    {
        ResetRecorderStream(_recorder);

        if (_recorder is null)
        {
            return;
        }

        _recorder.OnRecordFailed -= OnRecordFailed;
        _recorder.OnRecordDeviceRestore -= OnRecordDeviceRestore;
        _recorder.Dispose();
        _recorder = null;
    }

    private void ResetRecorderStream(IAudioRecorder? recorder)
    {
        if (_recorderStream is null)
        {
            return;
        }

        if (recorder is not null)
        {
            recorder.OnRecordFinished -= _recorderStream.RecordFinished;
        }

        _recorderStream.AudioDataRecorded -= AudioDataRecorded;
        _recorderStream.Dispose();
        _recorderStream = null;
    }

    public void Dispose()
    {
        ResetRecorder();

        _sequencer = null!;
        _recorderFactory = null!;

        _disposables.ForEach(disposable => disposable.Dispose());
        _disposables.Clear();
        _disposables = null!;
    }
}