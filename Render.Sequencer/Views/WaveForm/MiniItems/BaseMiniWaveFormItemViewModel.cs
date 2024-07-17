using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Painters;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Sequencer.Core.Utils.Helpers;

namespace Render.Sequencer.Views.WaveForm.MiniItems;

public class BaseMiniWaveFormItemViewModel : BaseViewModel
{
    [Reactive]
    public float[] Samples { get; set; }

    [Reactive]
    public double Width { get; set; }

    [Reactive]
    public bool IsSelected { get; set; }

    [Reactive]
    public InternalSequencer Sequencer { get; private set; }

    [Reactive]
    public SequencerAudio SequencerAudio { get; set; }

    [Reactive]
    public BaseWaveformPainter Painter { get; private set; }

    internal BaseMiniWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
    {
        Sequencer = sequencer;
        SequencerAudio = audio;
        Samples = Array.Empty<float>();
        Painter = new RecorderMiniWaveformPainter();

        SetupListeners();
    }

    protected virtual void SetupListeners()
    {
        SequencerAudio
            .WhenAnyValue(audio => audio.TotalSamples)
            .Subscribe(UpdateSamples)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.TotalDuration,
                sequencer => sequencer.Width,
                sequencer => sequencer.Mode)
            .Where(_ => Sequencer.IsNotRecording())
            .Where(((double _, double Width, SequencerMode __) options) => options.Width > 0 && SequencerAudio.IsTemp is false)
            .Subscribe(((double TotalDuration, double Width, SequencerMode mode) options) => WhenSequencerChanged(options.TotalDuration, options.Width, options.mode))
            .ToDisposables(Disposables);

        Sequencer
            .WhenRecordingStarted()
            .Where(_ => Sequencer.IsInAppendRecorderMode() is false)
            .Subscribe(_ => Width = Sequencer.Width)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.Width)
            .Where(_ => Sequencer.IsRecorder() && Sequencer.EditorMode is false)
            .Subscribe(_ => Width = Sequencer.Width)
            .ToDisposables(Disposables);
    }

    private void UpdateSamples(float[] samples)
    {
        Samples = samples;
    }

    protected virtual void WhenSequencerChanged(double totalDuration, double Width, SequencerMode Mode)
    {
        UpdateWidth(SequencerAudio.Duration, Width);
    }

    private void UpdateWidth(double duration, double totalWidth)
    {
        var ratio = SamplesHelper.GetSecToDipRatio(Sequencer.TotalDuration, totalWidth);
        Width = ratio == 0 ? default : duration / ratio;
    }

    public override void Dispose()
    {
        SequencerAudio = null!;

        base.Dispose();
    }
}