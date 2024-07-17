using System.Collections.ObjectModel;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DynamicData;
using DynamicData.Binding;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Views.Flags.Base;

namespace Render.Sequencer.Views.WaveForm.MiniItems;

public class MiniWaveFormItemViewModel : BaseMiniWaveFormItemViewModel
{
    private readonly int _audioHashCode;

    [Reactive]
    public bool HasFlags { get; set; }

    private ReadOnlyObservableCollection<BaseFlagViewModel>? _flags;
    public ReadOnlyObservableCollection<BaseFlagViewModel> Flags
    {
        get => _flags!;
    }

    internal MiniWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
        : base(audio, sequencer)
    {
        _audioHashCode = audio.GetHashCode();
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
            .WhenAnyValue(viewModel => viewModel.HasFlags)
            .Where(hasFlags => hasFlags && Sequencer.IsNotRecording())
            .Subscribe(_ => UpdateFlagsPosition())
            .ToDisposables(Disposables);

        Sequencer
            .WhenAppendRecordModeOff()
            .Subscribe(_ => UpdateFlagsPosition())
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.CurrentAudio)
            .Select(currentAudio => currentAudio == SequencerAudio)
            .BindTo(this, item => item.IsSelected)
            .ToDisposables(Disposables);

        Sequencer.Flags
            .ToObservableChangeSet()
            .Filter(flag => flag.AudioHashCode == _audioHashCode)
            .Transform(ComplementFlag)
            .Bind(out _flags)
            .Subscribe()
            .ToDisposables(Disposables);
    }

    protected override void WhenSequencerChanged(double totalDuration, double Width, SequencerMode Mode)
    {
        base.WhenSequencerChanged(totalDuration, Width, Mode);

        UpdateFlagsPosition();
    }

    private BaseFlagViewModel ComplementFlag(BaseFlagViewModel flagViewModel)
    {
        flagViewModel.MiniSecToDipPositionRatio = SequencerAudio.GetSecToDipRatio(Width);
        return flagViewModel;
    }

    private void UpdateFlagsPosition()
    {
        if (Flags.IsNullOrEmpty())
        {
            return;
        }

        Flags.ForEach(flag => flag.MiniSecToDipPositionRatio = SequencerAudio.GetSecToDipRatio(Width));
    }
}