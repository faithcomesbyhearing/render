using ReactiveUI;
using Render.Models.Audio;
using System.Reactive;

namespace Render.Sequencer.Contracts.Interfaces;

public interface ISequencerCommonRecorderViewModel
{
    AudioDetails AudioDetails { get; }

    ReactiveCommand<Unit, bool>? HasRecordPermissionCommand { get; set; }

    ReactiveCommand<Unit, Unit>? OnRecordFailedCommand { get; set; }

    ReactiveCommand<Unit, Unit>? OnRecordDeviceRestoreCommand { get; set; }
}
