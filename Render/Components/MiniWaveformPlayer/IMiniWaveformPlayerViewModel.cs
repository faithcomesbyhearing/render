using Render.Components.BarPlayer;
using Render.Services.AudioServices;

namespace Render.Components.MiniWaveformPlayer
{
    public interface IMiniWaveformPlayerViewModel: IBarPlayerViewModel
    { 
        IAudioPlayerService AudioPlayerService { get; }
    }
}