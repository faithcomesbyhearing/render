using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Flags;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Views.WaveForm.Items;

public class WaveFormItemViewModel : BaseWaveFormItemViewModel
{
    private readonly int _audioHashCode;

    [Reactive]
    public FlagType FlagsType { get; private set; }

    [Reactive]
    public bool HasFlags { get; set; }

    private ReadOnlyObservableCollection<BaseFlagViewModel>? _flags;
    public ReadOnlyObservableCollection<BaseFlagViewModel> Flags
    {
        get => _flags!;
    }

    internal ReactiveCommand<BaseFlagViewModel, Unit> FlagTappedCommand { get; }

    internal WaveFormItemViewModel(
        SequencerAudio audio,
        InternalSequencer sequencer,
        FlagType flagType)
        : base(audio, sequencer)
    {
        _audioHashCode = audio.GetHashCode();

        FlagsType = flagType;
        SequencerAudio = audio;
        FlagTappedCommand = ReactiveCommand.CreateFromTask<BaseFlagViewModel>(FlagTappedAsync);
    }

    protected override void SetupListeners()
    {
        base.SetupListeners();

        this
            .WhenAnyValue(viewModel => 
                viewModel.Sequencer.State, 
                viewModel => viewModel.Samples, 
                viewModel => viewModel.Sequencer.AppendRecordMode)
            .Where(_ => Sequencer.IsNotRecording())
            .Subscribe(((SequencerState _, float[] Samples, bool AppendRecordMode) options) => 
                HasFlags = options.Samples.IsEmpty() is false && options.AppendRecordMode is false)
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(viewModel => viewModel.IsSelected)
            .Subscribe(AddRemoveFlagRequestHandler)
            .ToDisposables(Disposables);

        Sequencer.Flags
            .ToObservableChangeSet()
            .Filter(flag => flag.AudioHashCode == _audioHashCode)
            .Transform(ComplementFlag)
            .Bind(out _flags)
            .Subscribe()
            .ToDisposables(Disposables);

        Sequencer
            .WhenAppendRecordModeOn()
            .Subscribe(_ => UpdateWidth(Duration))
            .ToDisposables(Disposables);

        Sequencer
            .WhenAppendRecordModeOff()
            .Subscribe(_ => UpdateFlagsPosition())
            .ToDisposables(Disposables);

        this
            .WhenAnyValue(viewModel => viewModel.HasFlags)
            .Where(hasFlags => hasFlags && Sequencer.IsNotRecording())
            .Subscribe(_ => UpdateFlagsPosition())
            .ToDisposables(Disposables);
    }

    protected override void WhenSequencerChanged(double totalDuration, double Width, SequencerMode Mode)
    {
        base.WhenSequencerChanged(totalDuration, Width, Mode);

        UpdateFlagsPosition();
    }

    /// <summary>
    /// Need to stop playing before flag openning. See BUG 26700 for details.
    /// </summary>
    private async Task FlagTappedAsync(BaseFlagViewModel flag)
    {
        var tappedPosition = Sequencer.TotalCurrentPosition;
        var tappedInsideBoundaries = tappedPosition > SequencerAudio.StartPosition && tappedPosition < SequencerAudio.EndPosition;

        if (tappedInsideBoundaries)
        {
            Sequencer.InternalPlayer.Pause();

            await Sequencer.TapFlagCommand.SafeExecute(flag);
        }
    }

    private void AddRemoveFlagRequestHandler(bool isSelected)
    {
        Sequencer.RequestNewFlag -= AddFlag;

        if (isSelected)
        {
            Sequencer.RequestNewFlag += AddFlag;
        }
    }

    internal Task<bool> AddFlag(BaseFlagViewModel flagViewModel)
    {
        return Sequencer.AddFlagCommand.SafeExecute(flagViewModel).ToTask();
    }

    private BaseFlagViewModel ComplementFlag(BaseFlagViewModel flagViewModel)
    {
        flagViewModel.TapCommand = FlagTappedCommand;
        flagViewModel.SecToDipPositionRatio = SequencerAudio.GetSecToDipRatio(Width);

        if (flagViewModel is NoteFlagViewModel noteFlagViewModel)
        {
            noteFlagViewModel.Direction = FlagDirectionHelper.GetFlagDirection(
                flags: Flags.Where(flag => flag is NoteFlagViewModel).Cast<NoteFlagViewModel>(),
                position: noteFlagViewModel.PositionDip,
                width: Width);
        }

        if (flagViewModel is MarkerFlagViewModel markerFlagViewModel)
        {
            markerFlagViewModel.SetPositionDip(width: Width);
        }

        return flagViewModel;
    }

    /// <summary>
    /// Directions of loaded circular note flags might be differ from original direction.
    /// To avoid this misbehavior need to store original flags direction in the database.
    /// Flags direction might change during resizing as well.
    /// </summary>
    private void UpdateFlagsPosition()
    {
        if (Flags.IsNullOrEmpty())
        {
            return;
        }

        Flags.ForEach(flag => flag.SecToDipPositionRatio = SequencerAudio.GetSecToDipRatio(Width));

        FlagDirectionHelper.UpdateFlagsDirection(
            waveformWidth: Width,
            noteFlags: Flags
                .Where(flag => flag is NoteFlagViewModel)
                .Cast<NoteFlagViewModel>()
                .OrderBy(flag => flag.PositionSec)
                .ToArray());

        var markerFlags = Flags
            .Where(flag => flag is MarkerFlagViewModel)
            .Cast<MarkerFlagViewModel>()
            .OrderBy(flag => flag.PositionSec)
            .ToArray();

        for (int i = 0; i < markerFlags.Length; i++)
        {
            markerFlags[i].SetPositionDip(width: Width);
        }
    }

    public override void Dispose()
    {
        Sequencer.RequestNewFlag -= AddFlag;

        base.Dispose();
    }
}