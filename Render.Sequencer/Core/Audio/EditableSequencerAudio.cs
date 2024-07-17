using Render.Sequencer.Contracts.Models;
using Render.Services.AudioPlugins.AudioPlayer;

namespace Render.Sequencer.Core.Audio;

public class EditableSequencerAudio : SequencerAudio
{
    private const double MinAudioChunkInSeconds = 0.5d;

    public bool CanCut 
    { 
        get => CurrentPosition >= MinAudioChunkInSeconds && 
               Duration - CurrentPosition >= MinAudioChunkInSeconds; 
    }

    internal EditableSequencerAudio(EditableAudioModel audio, Func<IAudioPlayer> playerFactory) 
        : base(audio, playerFactory)
    {
    }

    internal EditableAudioModel[] Split(double position)
    {
        if(CanCut is false)
        {
            return Array.Empty<EditableAudioModel>();
        }

        if (Audio is not EditableAudioModel audio)
        {
            audio = EditableAudioModel.Create(Audio.Path!);
            audio.StartTime = 0;
            audio.EndTime = Duration;
        }

        var first = EditableAudioModel.Create(Audio.Path!);
        var last = EditableAudioModel.Create(Audio.Path!);

        first.TotalDuration = audio.TotalDuration ?? Duration;
        first.StartTime = audio.StartTime; 
        first.EndTime = audio.StartTime + position;
        
        last.TotalDuration = audio.TotalDuration ?? Duration;
        last.StartTime = first.EndTime;
        last.EndTime = audio.EndTime;

        return new[] { first, last };
    }

    internal EditableAudioModel[] AppendOrPrependAudio(double position, EditableAudioModel audioToInsert)
    {
        if(Audio is not EditableAudioModel audio)
        {
            audio = EditableAudioModel.Create(Audio.Path!);
        }

        if(Duration / 2 > position)
        {
            //prepend
            return new[] { audioToInsert, audio }; 
        }
        else
        {
            //append
            return new[] { audio, audioToInsert };
        }
    }

    internal override bool TrySetAudio(AudioModel audio, double startPosition)
    {
        audio = EditableAudioModel.Create(audio.Path!);
        return base.TrySetAudio(audio, startPosition);
    }

    internal override bool Init(double startPosition)
    {
        var isSuccess = base.Init(startPosition);

        if(isSuccess is false)
        {
            return false;
        }

        if (Audio is not EditableAudioModel audio)
        {
            audio = EditableAudioModel.Create(Audio.Path!);
        }

        Player.StartTime = audio.StartTime;
        Player.EndTime = audio.EndTime;
        Duration = Player.Duration;

        audio.TotalDuration = audio.TotalDuration ?? Duration;
        audio.StartTime = audio.StartTime ?? 0;
        audio.EndTime = audio.EndTime ?? Duration;

        return isSuccess;
    }
}
