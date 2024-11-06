using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Render.Services.AudioServices;

namespace Render.Platforms.Kernel.AudioTools
{
    public class AudioDeviceMonitor : IAudioDeviceMonitor
    {
        public event Action<string> AudioInputDeviceChanged;
        public event Action<string> AudioOutputDeviceChanged;
        public event Action<string> UnknownDeviceChanged;

        public bool HasActiveAudioOutputDevice { get; }

        public bool HasActiveAudioInputDevice { get; }

        public string GetCurrentInputDeviceId()
        {
            return string.Empty;
        }

        public string GetCurrentOuputDeviceId()
        {
            return string.Empty;
        }
    }
}