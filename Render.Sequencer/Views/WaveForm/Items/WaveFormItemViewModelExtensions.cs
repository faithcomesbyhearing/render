using System.Reactive.Linq;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Helpers;
using Render.Sequencer.Views.Flags;

namespace Render.Sequencer.Views.WaveForm.Items;

internal static class WaveFormItemViewModelExtensions
{
    /// <summary>
    /// Calculates max width of the wave form item, 
    /// based on device screen width and audio duration.
    /// </summary>
    internal static int GetMaxWidth(this BaseWaveFormItemViewModel waveFormItem)
    {
        var maxWidthPerSecond = SamplesHelper.GetWidthPerSecond(
            width: /*DeviceDisplay.MainDisplayInfo.Width*/ 1920,
            forRecording: false,
            secondsOnScreen: InternalPlayer.SecondsOnScreen,
            durationSec: waveFormItem.Sequencer.TotalDuration);

        var maxWidth = maxWidthPerSecond *  waveFormItem.SequencerAudio.Duration;
        return (int)Math.Floor(maxWidth <= 0 ? waveFormItem.Sequencer.MaxWidth : maxWidth);
    }

    internal static NoteFlagModel CreateNoteFlagModel(this WaveFormItemViewModel waveFormItem)
    {
        return new(waveFormItem.SequencerAudio.CurrentPosition, required: false, read: true);
    }

    internal static MarkerFlagModel CreateMarkerFlagModel(this WaveFormItemViewModel waveFormItem)
    {
        return new(waveFormItem.SequencerAudio.CurrentPosition, required: false, symbol: 0.ToString(), read: true);
    }

    internal static MarkerFlagViewModel CreateMarkerFlagViewModel(this WaveFormItemViewModel waveFormItem)
    {
        return new()
        {
            AbsPositionSec = 0,
            PositionSec = waveFormItem.SequencerAudio.CurrentPosition,
            TapCommand = waveFormItem.FlagTappedCommand,
            SecToDipPositionRatio = waveFormItem.SequencerAudio.GetSecToDipRatio(waveFormItem.Width),
            Symbol = 0.ToString(),
        };
    }

    internal static NoteFlagViewModel CreateNoteFlagViewModel(this WaveFormItemViewModel waveFormItem)
    {
        var flagViewModel = new NoteFlagViewModel()
        {
            AbsPositionSec = 0,
            PositionSec = waveFormItem.SequencerAudio.CurrentPosition,
            TapCommand = waveFormItem.FlagTappedCommand,
            SecToDipPositionRatio = waveFormItem.SequencerAudio.GetSecToDipRatio(waveFormItem.Width),
        };

        flagViewModel.Direction = FlagDirectionHelper.GetFlagDirection(
            flags: waveFormItem.Flags.Where(f => f is NoteFlagViewModel).Cast<NoteFlagViewModel>(),
            position: flagViewModel.PositionDip,
            width: waveFormItem.Width);

        return flagViewModel;
    }
}