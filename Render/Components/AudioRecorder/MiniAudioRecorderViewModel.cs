using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Interfaces.WrappersAndExtensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Resources;
using Render.Resources.Localization;
using Render.Services.AudioServices;
using Render.Services.WaveformService;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Timers;

namespace Render.Components.AudioRecorder;

public class MiniAudioRecorderViewModel : ActionViewModelBase, IMiniAudioRecorderViewModel
{
    public const double TimerIntervalInMilliseconds = 100;

    protected readonly IWaveFormService WaveFormService;
    protected readonly IAudioRecorderServiceWrapper AudioRecorder;

    public IAudioPlayerService AudioPlayer { get; set; }

    protected Audio Audio { get; private set; }

    protected Stream Stream { get; set; }

    public string AudioFilePath { get; protected set; }

    [Reactive]
    public AudioRecorderState AudioRecorderState { get; protected set; }

    [Reactive]
    public double CurrentPosition { get; set; }

    [Reactive]
    public double RecordedFileDuration { get; set; }

    [Reactive]
    public float[] AudioSamples { get; set; }

    protected float[] SavedData { get; set; } = Array.Empty<float>();

    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }
    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    protected System.Timers.Timer RecordingTimerForStream { get; set; }

    public MiniAudioRecorderViewModel(Audio audio, IViewModelContextProvider viewModelContextProvider)
        : base(ActionState.Optional, "MiniAudioRecorder", viewModelContextProvider)
    {
        Audio = audio;

        WaveFormService = ViewModelContextProvider.GetWaveFormService();
        AudioPlayer = ViewModelContextProvider.GetAudioPlayerService(Pause);
        AudioRecorder = ViewModelContextProvider.GetAudioRecorderService(
            StopRecordingAsync,
            ProcessRecordFailedAsync,
            ProcessRecordDeviceRestoreAsync);

        StartRecordingCommand = ReactiveCommand.CreateFromTask(StartRecordingAsync);
        StopRecordingCommand = ReactiveCommand.CreateFromTask(StopRecordingAsync);
        PauseCommand = ReactiveCommand.Create(Pause);
        PlayCommand = ReactiveCommand.Create(Play);
        DeleteCommand = ReactiveCommand.Create(DeleteAudio);

        AudioSamples = Array.Empty<float>();

        ResetRecordingTimer();
        SetupListeners();
    }

    public void Play()
    {
        AudioPlayer.Play();
        AudioRecorderState = AudioRecorderState.PlayingAudio;
    }

    public void Pause()
    {
        AudioPlayer.Pause();
        AudioRecorderState = AudioRecorderState.CanPlayAudio;
    }

    public async Task StartRecordingAsync()
    {
        var modalService = ViewModelContextProvider.GetModalService();

        try
        {
            var essentials = ViewModelContextProvider.GetEssentials();
            var permissionsStatus = await essentials.CheckForAudioPermissions();
            if (!permissionsStatus)
            {
                permissionsStatus = await essentials.AskForAudioPermissions();
                //Tell the user they need to give permissions and try again
                Logger.LogInfo("Requested audio permissions from user");
            }

            if(permissionsStatus is false)
            {
                await modalService.ShowInfoModal(Icon.MicrophoneWarning, AppResources.MicrophoneAccessTitle, AppResources.MicrophoneAccessMessage);
                return;
            }

            await AudioRecorder.StartRecording();
            Stream = AudioRecorder.GetAudioFileStream();
            RecordingTimerForStream.Start();
            AudioRecorderState = AudioRecorderState.Recording;
            Logger.LogInfo("Audio started recording");
        }
		catch (Exception e)
		{
			await modalService.ShowInfoModal(Icon.MicrophoneNotFound, AppResources.MicNotConnectedTitle, AppResources.MicNotConnectedMessage);
			Logger.LogError(e);
			return;
		}
	}

    public async Task StopRecordingAsync()
    {
        if (AudioRecorder.IsRecording is false)
        {
            return;
        }

        RecordingTimerForStream.Stop();
        await AudioRecorder.StopRecording();
        Stream.Close();
        AudioFilePath = AudioRecorder.GetAudioFilePath();

        if (AudioFilePath == null)
        {
            DeleteAudio();
            ResetRecordingTimer();
        }
        else
        {
            InitAudio();
        }

        Logger.LogInfo("Audio recording stopped");
    }

    public IObservable<Unit> StopRecorderActivity()
    {
        switch (AudioRecorderState)
        {
            case AudioRecorderState.Recording:
                return StopRecordingCommand.Execute();
            case AudioRecorderState.PlayingAudio:
                return PauseCommand.Execute();
            default:
                return Task.FromResult<Unit>(default).ToObservable();
        }
    }

    protected virtual void InitAudio()
    {
        var stream = new FileStream(AudioFilePath, FileMode.Open, FileAccess.Read);
        AudioSamples = Array.Empty<float>();
        BuildAudioSamples(stream, false);
        stream.Seek(0, SeekOrigin.Begin);
        AudioPlayer.Load(stream, null, true);
        RecordedFileDuration = AudioPlayer.Duration;
        AudioRecorderState = AudioRecorderState.CanPlayAudio;
    }

    protected virtual void DeleteAudio()
    {
        CurrentPosition = 0;
        Audio = new Audio(Audio.ScopeId, Audio.ProjectId, Audio.ParentId);
        AudioFilePath = null;
        AudioSamples = Array.Empty<float>();
        AudioPlayer.Unload();
        AudioPlayer.SimpleAudioPlayer.Unload();
        RecordedFileDuration = 0;
        AudioRecorderState = AudioRecorderState.NoAudio;
    }

    public void SetAudio(Audio audio)
    {
        DeleteAudio();
        Audio = audio;
    }

    public async Task<Audio> GetAudio()
    {
        if (Audio.HasAudio || AudioFilePath is null || File.Exists(AudioFilePath) is false)
        {
            return Audio;
        }

        var audioEncodingService = ViewModelContextProvider.GetAudioEncodingService();

        using (var stream = new FileStream(AudioFilePath, FileMode.Open, FileAccess.Read))
        {
            var opus = await audioEncodingService.ConvertWavToOpusAsync(stream, 48000, 1);
            Audio.SetAudio(opus);
        }

        return Audio;
    }

    private void ResetRecordingTimer()
    {
        RecordingTimerForStream = new System.Timers.Timer(TimerIntervalInMilliseconds);
        RecordingTimerForStream.Elapsed += WatchStreamAsync;
        RecordingTimerForStream.AutoReset = true;
    }

    private async void WatchStreamAsync(object sender, ElapsedEventArgs e)
    {
        try
        {
            if (AudioRecorder.IsRecording && Stream.CanRead)
            {
                RecordedFileDuration += TimerIntervalInMilliseconds / 1000;

                var length = Stream.Length - Stream.Position;
                var tempBuffer = new byte[length];
                var total = await Stream.ReadAsync(tempBuffer, 0, (int)length);

                if (total == 0)
                {
                    return;
                }

                BuildAudioSamples(tempBuffer, true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void BuildAudioSamples(Stream stream, bool isRecording)
    {
        const int wavBufferSize = 5000000; // 5 Megabytes
        byte[] wavBuffer = new byte[wavBufferSize];
        int bytesRead;
        while ((bytesRead = stream.Read(wavBuffer, 0, wavBuffer.Length)) > 0)
        {
            var bytes = bytesRead != wavBuffer.Length
                ? wavBuffer.Take(bytesRead).ToArray()
                : wavBuffer;

            BuildAudioSamples(bytes, isRecording);
        }
    }

    private void BuildAudioSamples(byte[] audioData, bool isRecording)
    {
        try
        {
            var bitPerSample =
                AudioRecorder.AudioStreamDetails?.BitsPerSample ?? 16; //16, 24, 32 not allow 8 bits, normally 16
            var sampleRate = AudioRecorder.AudioStreamDetails?.SampleRate ?? 48000;

            var audioDataAsFloat = WaveFormService.ConvertAudioDataToFloat(audioData, bitPerSample);

            //If we have in memory data that is not in the bars yet, add it to the beginning of our array to 
            //make bars out of it.
            if (SavedData.Length > 0)
            {
                audioDataAsFloat = SavedData.Concat(audioDataAsFloat).ToArray();
                SavedData = Array.Empty<float>();
            }

            if (audioDataAsFloat is null)
            {
                return;
            }

            int numberOfBars;
            int samplesPerBar;
            var widthPerSecond = GetWidthPerSecond();
            if (isRecording)
            {
                samplesPerBar = (int)(sampleRate / widthPerSecond);
                numberOfBars = audioDataAsFloat.Length / samplesPerBar;
            }
            else
            {
                //Sometimes, for very short records RecordedFileDuration might be 0,
                //to avoid divizion by zero exception and empty waveform set minimum duration
                var duration = RecordedFileDuration is 0 ? 0.1 : RecordedFileDuration;
                numberOfBars = (int)(widthPerSecond * duration);
                samplesPerBar = audioDataAsFloat.Length / numberOfBars;
            }

            if (numberOfBars == 0)
            {
                return;
            }

            float[] barArray = WaveFormService.CreateBars(audioDataAsFloat, numberOfBars, samplesPerBar);

            if (isRecording && RecordedFileDuration >= 30)
            {
                //if the audio samples hits a limit we will truncate the data
                var audioToTake = Math.Max(0, (int)(Application.Current.MainPage.Width - barArray.Length));
                
                //Sometimes, this method fires just before the edge of the screen, meaning we have less
                //samples than bars drawn, so we need to adjust to not crash in that scenario
                audioToTake = audioToTake > AudioSamples.Length ? AudioSamples.Length : audioToTake;
                
                var temp = new float[audioToTake];
                var sourceIndex = AudioSamples.Length - audioToTake;

                Array.Copy(AudioSamples, sourceIndex, temp, 0, audioToTake);
                AudioSamples = temp.Concat(barArray).ToArray();
            }
            else
            {
                AudioSamples = AudioSamples.Concat(barArray).ToArray();
            }

            //If we have more data than will neatly fit into the bars, save off the extra to be added in 
            //next time we get more data from the stream.
            if (numberOfBars * samplesPerBar < audioDataAsFloat.Length && isRecording)
            {
                var startNumber = numberOfBars * samplesPerBar;
                var endNumber = audioDataAsFloat.Length;
                SavedData = audioDataAsFloat.Skip(startNumber).Take(endNumber - startNumber).ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private double GetWidthPerSecond()
    {
        if (Application.Current == null)
        {
            return 1;
        }

        var width = Application.Current.MainPage.Width;

        return width / 30d;
    }

    private void SetupListeners()
    {
        Disposables.Add(this
            .WhenAnyValue(vm => vm.AudioPlayer.CurrentPosition)
            .BindTo(this, vm => vm.CurrentPosition));

        Disposables.Add(this
            .WhenAnyValue(vm => vm.AudioPlayer.AudioPlayerState)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
            {
                if (state == AudioPlayerState.Loaded)
                {
                    AudioPlayer.Seek(0);
                    AudioRecorderState = AudioRecorderState.CanPlayAudio;
                }
            }));
    }

    private async Task ProcessRecordFailedAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await StopRecordingAsync();

            var modalService = ViewModelContextProvider.GetModalService();
            await modalService.ShowInfoModal(Icon.MicrophoneNotFound, AppResources.MicNotConnectedTitle, AppResources.MicNotConnectedMessage);
            Logger.LogInfo("Microphone is not connected");
        });
    }

    private async Task ProcessRecordDeviceRestoreAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var modalService = ViewModelContextProvider.GetModalService();
            modalService.Close(DialogResult.Ok);
            Logger.LogInfo("Microphone is restored");
        });
    }

    public override void Dispose()
    {
        Audio = null;
        Stream = null;

        RecordingTimerForStream.Dispose();

        StartRecordingCommand.Dispose();
        StopRecordingCommand.Dispose();
        PlayCommand.Dispose();
        PauseCommand.Dispose();
        DeleteCommand.Dispose();
        
        AudioRecorder?.Dispose();

        AudioPlayer.Unload();
        AudioPlayer.Dispose();

        base.Dispose();
    }
}