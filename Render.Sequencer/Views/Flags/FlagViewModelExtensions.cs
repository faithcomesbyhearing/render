using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Views.Flags;

internal static class FlagViewModelExtensions
{
    internal static FlagModel ToFlagModel(this BaseFlagViewModel flagViewModel)
    {
        return flagViewModel switch
        {
            NoteFlagViewModel noteFlagViewModel => noteFlagViewModel.ToNoteFlagModel(),
            MarkerFlagViewModel markerFlagViewModel => markerFlagViewModel.ToMarkerFlagModel(),
            _ => throw new NotImplementedException(),
        };
    }

    internal static NoteFlagModel ToNoteFlagModel(this NoteFlagViewModel noteFlagViewModel)
    {
        return new NoteFlagModel(
            position: noteFlagViewModel.PositionSec,
            required: noteFlagViewModel.Option is ItemOption.Required,
            read: noteFlagViewModel.State is FlagState.Read)
        {
            Direction = noteFlagViewModel.Direction,
        };
    }

    internal static MarkerFlagModel ToMarkerFlagModel(this MarkerFlagViewModel markerFlagViewModel)
    {
        return new MarkerFlagModel(
            position: markerFlagViewModel.PositionSec,
            required: markerFlagViewModel.Option is ItemOption.Required,
            symbol: markerFlagViewModel.Symbol,
            read: markerFlagViewModel.State is FlagState.Read);
    }
}
