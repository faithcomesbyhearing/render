using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Audio;
using Render.Sequencer.Core.Base;
using Render.Sequencer.Core.Painters;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Views.WaveForm.Items;

public class BaseWaveFormItemViewModel : BaseViewModel
{
    [Reactive]
    public float[] Samples { get; set; }

    [Reactive]
    public double Duration { get; set; }

    [Reactive]
    public double Width { get; set; }

    /// <summary>
    /// Possible maximum width of WaveFormItem control.
    /// Depends on device screen size. Static value.
    /// TODO: React to device display resolution changes.
    /// </summary>
    [Reactive]
    public double MaxWidth { get; set; }

    [Reactive]
    public bool IsSelected { get; set; }

    [Reactive]
    public InternalSequencer Sequencer { get; private set; }

    [Reactive]
    public SequencerAudio SequencerAudio { get; set; }

    [Reactive]
    public BaseWaveformPainter Painter { get; private set; }

    [Reactive]
    public ICommand UpdateCanvasCommand { get; set; }

    internal BaseWaveFormItemViewModel(InternalSequencer sequencer)
        : this(SequencerAudio.Empty(), sequencer) { }

    internal BaseWaveFormItemViewModel(SequencerAudio audio, InternalSequencer sequencer)
    {
        Sequencer = sequencer;
        SequencerAudio = audio;
        MaxWidth = this.GetMaxWidth();
        Samples = Array.Empty<float>();
        Painter = new InterpolatingWaveformPainter(MaxWidth);

        UpdateCanvasCommand = ReactiveCommand.Create(UpdateCanvas);

        SetupListeners();
        UpdateCanvas();
    }

    protected virtual void SetupListeners()
    {
        SequencerAudio
            .WhenAnyValue(audio => audio.TotalSamples)
            .Where(_ => Sequencer.IsNotRecording() && Sequencer.IsNotInAppendRecorderMode())
            .Subscribe(UpdateSamples)
            .ToDisposables(Disposables);

        SequencerAudio
            .WhenAnyValue(audio => audio.LastSamples)
            .Where(_ => Sequencer.IsRecording() || Sequencer.IsInAppendRecorderMode())
            .Subscribe(UpdateSamples)
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(
                sequencer => sequencer.TotalDuration,
                sequencer => sequencer.Width,
                sequencer => sequencer.Mode)
            .Where(_ => Sequencer.IsNotRecording())
            .Where(((double _, double Width, SequencerMode Mode) options) => options.Width > 0 && SequencerAudio.IsTemp is false)
            .Subscribe(((double TotalDuration, double Width, SequencerMode Mode) options) => WhenSequencerChanged(options.TotalDuration, options.Width, options.Mode))
            .ToDisposables(Disposables);

        Sequencer
            .WhenAnyValue(sequencer => sequencer.Width)
            .Where(_ => Sequencer.IsRecording())
            .Subscribe(_ => Width = Sequencer.Width)
            .ToDisposables(Disposables);

        Sequencer
            .WhenRecordingStarted()
            .Subscribe(_ => UpdateMaxWidth(matchSequencer: true))
            .ToDisposables(Disposables);

        Sequencer
            .WhenRecordingStopped()
            .Subscribe(_ =>
            {
                UpdateWidth(Sequencer.TotalDuration);
                UpdateMaxWidth(matchSequencer: false);
                SequencerAudio.RefreshPlayerSamples(Sequencer.GetPlayerBuildSamplesParams(SequencerAudio.Duration));
            })
            .ToDisposables(Disposables);

        Sequencer
            .WhenAppendRecordModeOn()
            .Subscribe(_ =>
            {
                UpdateMaxWidth(matchSequencer: true);
                SequencerAudio.RefreshRecorderSamples(Sequencer.GetRecordingBuildSamplesParams(Sequencer.TotalDuration));
            })
            .ToDisposables(Disposables);
    }

    protected virtual void WhenSequencerChanged(double totalDuration, double Width, SequencerMode Mode)
    {
        UpdateWidth(SequencerAudio.Duration);
    }

    protected virtual void UpdateWidth(double duration)
    {
        if (Sequencer.AppendRecordMode)
        {
            Width = Sequencer.Width;
            return;
        }

        var widthPerSecond = Sequencer.GetWidthPerSecond(false);
        var width = widthPerSecond * duration;

        if (Sequencer.IsInRecorderMode())
        {
            width = width > Sequencer.Width ? width : Sequencer.Width;
        }

        Width = width;
    }

    protected virtual void UpdateCanvas()
    {
        if (Sequencer.IsNotRecording())
        {
            UpdateCanvasInternal();
        }
    }

    private void UpdateCanvasInternal()
    {
        if (SequencerAudio.IsTemp)
        {
            return;
        }

        if (Sequencer.AppendRecordMode)
        {
            UpdateMaxWidth(matchSequencer: true);
            SequencerAudio.RefreshRecorderSamples(Sequencer.GetRecordingBuildSamplesParams(Duration));
        }
        else
        {
            SequencerAudio.RefreshPlayerSamples(Sequencer.GetPlayerBuildSamplesParams(
                duration: Duration,
                width: Duration > Sequencer.SecondsOnScreen ? Sequencer.MaxWidth : MaxWidth));
        }
    }

    private void UpdateSamples(float[] samples)
    {
        Duration = SequencerAudio.Duration;
        Samples = samples;
    }

    /// <summary>
    /// Calculates possible maximum width of the WaveFormItem.
    /// If 'matchSequencer' parameter is true, just setts max width of the sequencer.
    /// Usually that means maximum device screen width. 
    /// Used in recorder mode to constraint item width to the whole available visible area only.
    /// </summary>
    /// <param name="matchSequencer">Whether to use Sequencer MaxWidth or calculate the value based on audio duration.</param>
    private void UpdateMaxWidth(bool matchSequencer)
    {
        MaxWidth = matchSequencer ? Sequencer.MaxWidth : this.GetMaxWidth();

        if (Painter is InterpolatingWaveformPainter painter)
        {
            painter.MaxWaveFormWidth = MaxWidth;
        }
    }

    public override void Dispose()
    {
        SequencerAudio = null!;

        base.Dispose();
    }
}