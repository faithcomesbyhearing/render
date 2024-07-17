using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Views.Flags;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Contracts.Models;

internal static class FlagModelExtensions
{
    internal static BaseFlagViewModel ToViewModel(this FlagModel flagModel, double audioStartPosition, int audioHashCode)
    {
        return flagModel switch
        {
            NoteFlagModel noteFlagModel => noteFlagModel.ToNoteViewModel(audioStartPosition, audioHashCode),
            MarkerFlagModel markerFlagModel => markerFlagModel.ToMarkerFlagViewModel(audioStartPosition, audioHashCode),
            _ => throw new NotImplementedException(),
        };
    }

    internal static NoteFlagViewModel ToNoteViewModel(this NoteFlagModel flagModel, double audioStartPosition, int audioHashCode)
    {
        return new NoteFlagViewModel
        {
            Key = flagModel.Key,
            AudioHashCode = audioHashCode,
            PositionSec = flagModel.Position,
            AbsPositionSec = audioStartPosition + flagModel.Position,
            Option = flagModel.IsRequired ? ItemOption.Required : ItemOption.Optional,
            State = flagModel.IsRead ? FlagState.Read : FlagState.Unread,
        };
    }

    internal static MarkerFlagViewModel ToMarkerFlagViewModel(this MarkerFlagModel flagModel, double audioStartPosition, int audioHashCode)
    {
        return new MarkerFlagViewModel
        {
            Key = flagModel.Key,
            AudioHashCode = audioHashCode,
            PositionSec = flagModel.Position,
            AbsPositionSec = audioStartPosition + flagModel.Position,
            Option = flagModel.IsRequired ? ItemOption.Required : ItemOption.Optional,
            State = flagModel.IsRead ? FlagState.Read : FlagState.Unread,
            Symbol = flagModel.Symbol,
        };
    }
}