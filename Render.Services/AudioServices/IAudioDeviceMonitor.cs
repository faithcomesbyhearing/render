namespace Render.Services.AudioServices;

public interface IAudioDeviceMonitor
{
    event Action<string> AudioInputDeviceChanged;
    event Action<string> AudioOutputDeviceChanged;
    event Action<string> UnknownDeviceChanged;

    bool HasActiveAudioOutputDevice { get; }

    bool HasActiveAudioInputDevice { get; }

    string GetCurrentInputDeviceId();

    string GetCurrentOuputDeviceId();
}