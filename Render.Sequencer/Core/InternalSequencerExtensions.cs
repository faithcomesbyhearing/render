using ReactiveUI;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Errors;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Services.AudioPlugins.AudioPlayer;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Render.Sequencer.Core.Utils.Extensions;

internal static class InternalSequencerExtensions
{
    internal static void StopActivities(this InternalSequencer sequencer)
    {
        if (sequencer.IsPlaying())
        {
            sequencer.InternalPlayer.Stop();
        }

        if (sequencer.IsRecording())
        {
            _ = sequencer.InternalRecorder.StopAsync();
        }
    }

    internal static SequencerAudio[] CreateSequencerAudios(this InternalSequencer _,
        AudioModel[] audios,
        Func<IAudioPlayer> playerFactory)
    {
        var audioStartPosition = 0d;
        return audios
            .Select(audio =>
            {
                var sequencerAudio = new SequencerAudio(audio, playerFactory);
                sequencerAudio.Init(audioStartPosition);
                audioStartPosition += sequencerAudio.Duration;

                return sequencerAudio;
            }).ToArray();
    }

    internal static EditableSequencerAudio[] CreateSequencerAudios(this InternalSequencer _,
        EditableAudioModel[] audios,
        Func<IAudioPlayer> playerFactory)
    {
        var audioStartPosition = 0d;
        return audios
            .Select(audio =>
            {
                var sequencerAudio = new EditableSequencerAudio(audio, playerFactory);
                sequencerAudio.Init(audioStartPosition);
                audioStartPosition += sequencerAudio.Duration;

                return sequencerAudio;
            }).ToArray();
    }

    internal static void SetChainedPlayback(this InternalSequencer sequencer, SequencerAudio[] audios)
    {
        for (int i = 0; i < audios.Length - 1; i++)
        {
            var index = i;
            var audio = audios[index];
            var nextAudio = audios[index + 1];

            audio.PlaybackEnded += (s, e) =>
            {
                sequencer.CurrentAudio = audios[index + 1];
                nextAudio.Play();
            };
        }

        audios
            .Last()
            .PlaybackEnded += sequencer.GetPlaybackEndedCallback();
    }

    internal static EventHandler GetPlaybackEndedCallback(this InternalSequencer sequencer)
    {
        return (s, e) =>
        {
            sequencer.InternalPlayer.Timer.Stop();

            sequencer.State = SequencerState.Loaded;
            sequencer.Mode = SequencerMode.Player;
        };
    }

    internal static int GetCurrentAudioIndex(this InternalSequencer sequencer)
    {
        return sequencer.Audios.IndexOf(sequencer.CurrentAudio);
    }

    internal static double GetWidthPerSecond(this InternalSequencer sequencer, bool forRecording)
    {
        return SamplesHelper.GetWidthPerSecond(sequencer.Width, forRecording, sequencer.SecondsOnScreen, sequencer.TotalDuration);
    }

    /// <summary>
    /// Need to adjust recorder timer interval to have precise integer value 
    /// for number of bars to draw on canvas per interval. 
    /// Otherwise, during recording cycle, error accumulates and leads
    /// to inconsistency between the drawn bars on canvas and recording audio duration.
    /// </summary>
    internal static double GetFineTunedRecorderTimerInterval(this InternalSequencer sequencer)
    {
        var barsPerSecond = SamplesHelper.GetWidthPerSecond(sequencer.MaxWidth, true, sequencer.SecondsOnScreen, sequencer.TotalDuration);
        var barsPerDefaultTimerInterval = InternalRecorder.DefaultTimerIntervalMs / 1000 * barsPerSecond;
        var roundedBars = Math.Round(barsPerDefaultTimerInterval, 0, MidpointRounding.AwayFromZero);
        var tunedTimeIntervalMs = roundedBars / barsPerDefaultTimerInterval * 100;

        return tunedTimeIntervalMs;
    }

    internal static BuildSamplesParams GetRecordingBuildSamplesParams(this InternalSequencer sequencer, double totalDurationSec, double? chunkDurationSec=null)
    {
        var barsPerSecond = SamplesHelper.GetWidthPerSecond(sequencer.MaxWidth, true, InternalRecorder.SecondsOnScreen, totalDurationSec);
        return new BuildSamplesParams(
            bitPerSamples: sequencer.InternalRecorder.AudioDetails.BitsPerSample,
            duration: totalDurationSec,
            barsPerSecond: barsPerSecond,
            maxSecondsOnScreen: InternalRecorder.SecondsOnScreen,
            visibleBarsLength: sequencer.MaxWidth,
            buildSampleIntervalMs: chunkDurationSec is null ? 
                InternalRecorder.DefaultTimerIntervalMs : 
                chunkDurationSec.Value.ToMilliseconds());
    }

    internal static BuildSamplesParams GetPlayerBuildSamplesParams(this InternalSequencer sequencer, double duration, double? width = null)
    {
        var resultWidth = width is null ? sequencer.MaxWidth : width.Value;
        var barsPerSecond = SamplesHelper.GetWidthPerSecond(resultWidth, false, InternalPlayer.SecondsOnScreen, duration);
        return new BuildSamplesParams(
            bitPerSamples: 16,
            duration: duration,
            barsPerSecond: barsPerSecond,
            maxSecondsOnScreen: InternalPlayer.SecondsOnScreen,
            buildSampleIntervalMs: sequencer.InternalPlayer.TimerIntervalMs,
            visibleBarsLength: resultWidth);
    }

    internal static async Task<(bool IsSuccess, byte[] Audio)> TryReadAudioFromFileAsync(this InternalSequencer sequencer)
    {
        try
        {
            if (sequencer.InternalRecorder.Recorder is null)
            {
                return (IsSuccess: false, Audio: Array.Empty<byte>());
            }

            var filePath = sequencer.InternalRecorder.Recorder.GetAudioFilePath();
            var audio = await File.ReadAllBytesAsync(filePath);

            return (IsSuccess: true, Audio: audio);
        }
        catch (Exception e)
        {
            Debug.WriteLine(ErrorMessages.ReadingRecordError);
            Debug.WriteLine(e.Message);

            return (IsSuccess: false, Audio: Array.Empty<byte>());
        }
    }

    internal static IObservable<IList<SequencerState>> WhenRecordingStarted(this InternalSequencer sequencer)
    {
        return sequencer
            .WhenAnyValue(sequencer => sequencer.State)
            .Buffer(2, 1)
            .Where(stateBuffer =>
            {
                var previousState = stateBuffer[0];
                var currentState = stateBuffer[1];
                return currentState is SequencerState.Recording && previousState != currentState;
            });
    }

    internal static IObservable<IList<SequencerState>> WhenRecordingStopped(this InternalSequencer sequencer)
    {
        return sequencer
            .WhenAnyValue(sequencer => sequencer.State)
            .Buffer(2, 1)
            .Where(stateBuffer =>
            {
                var previousState = stateBuffer[0];
                var currentState = stateBuffer[1];
                return previousState is SequencerState.Recording && previousState != currentState;
            });
    }

    internal static IObservable<IList<bool>> WhenAppendRecordModeOn(this InternalSequencer sequencer)
    {
        return sequencer
            .WhenAnyValue(sequencer => sequencer.AppendRecordMode)
            .Buffer(2, 1)
            .Where(stateBuffer =>
            {
                var previousState = stateBuffer[0];
                var currentState = stateBuffer[1];
                return previousState is false && previousState != currentState;
            });
    }

    internal static IObservable<IList<bool>> WhenAppendRecordModeOff(this InternalSequencer sequencer)
    {
        return sequencer
            .WhenAnyValue(sequencer => sequencer.AppendRecordMode)
            .Buffer(2, 1)
            .Where(stateBuffer =>
            {
                var previousState = stateBuffer[0];
                var currentState = stateBuffer[1];
                return previousState is true && previousState != currentState;
            });
    }

    internal static void ForceScrollerToStart(this InternalSequencer sequencer)
    {
        sequencer.InputScrollX = 0;
    }

    internal static void ForceScrubberTo(this InternalSequencer sequencer, double position)
    {
        sequencer.TotalCurrentPosition = position;
        sequencer.CurrentPosition = position - sequencer.CurrentAudio.StartPosition;
    }

    internal static void ForceScrubberToStart(this InternalSequencer sequencer)
    {
        sequencer.ForceScrubberTo(0);
    }

    internal static bool IsActive(this InternalSequencer sequencer)
    {
        return sequencer.IsPlaying() || sequencer.IsRecording();
    }

    internal static bool IsNotActive(this InternalSequencer sequencer)
    {
        return sequencer.IsActive() is false;
    }

    internal static bool IsPlaying(this InternalSequencer sequencer)
    {
        return sequencer.State is SequencerState.Playing;
    }

    internal static bool IsNotPlaying(this InternalSequencer sequencer)
    {
        return sequencer.State is not SequencerState.Playing;
    }

    internal static bool IsRecording(this InternalSequencer sequencer)
    {
        return sequencer.State is SequencerState.Recording;
    }

    internal static bool IsNotRecording(this InternalSequencer sequencer)
    {
        return sequencer.State is not SequencerState.Recording;
    }

    internal static bool IsRecorder(this InternalSequencer sequencer)
    {
        return sequencer.InitialMode is SequencerMode.Recorder;
    }

    internal static bool IsPlayer(this InternalSequencer sequencer)
    {
        return sequencer.InitialMode is SequencerMode.Player;
    }

    internal static bool IsInRecorderMode(this InternalSequencer sequencer)
    {
        return sequencer.Mode is SequencerMode.Recorder;
    }

    internal static bool IsInPlayerMode(this InternalSequencer sequencer)
    {
        return sequencer.Mode is SequencerMode.Player;
    }

    internal static bool IsLoaded(this InternalSequencer sequencer)
    {
        return sequencer.State is SequencerState.Loaded;
    }

    internal static bool IsInAppendRecorderMode(this InternalSequencer sequencer)
    {
        return sequencer.AppendRecordMode;
    }

    internal static bool IsNotInAppendRecorderMode(this InternalSequencer sequencer)
    {
        return sequencer.AppendRecordMode is false;
    }
}