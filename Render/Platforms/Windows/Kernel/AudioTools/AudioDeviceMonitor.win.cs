using CSCore.CoreAudioAPI;
using Render.Services.AudioServices;

namespace Render.Platforms.Kernel.AudioTools
{
    public class AudioDeviceMonitor : IAudioDeviceMonitor
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
		private readonly MMNotificationClient _notificationClient;

        public event Action<string> AudioInputDeviceChanged;
        public event Action<string> AudioOutputDeviceChanged;
        public event Action<string> UnknownDeviceChanged;

        public bool HasActiveAudioOutputDevice
        {
            get => _deviceEnumerator
				.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active)
				.Any();
        }

        public bool HasActiveAudioInputDevice
        {
            get => _deviceEnumerator
				.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active)
				.Any();
        }

        public AudioDeviceMonitor()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient(_deviceEnumerator);
			_notificationClient.DefaultDeviceChanged += DefaultDeviceChanged;
        }

        public string GetCurrentInputDeviceId()
        {
            return HasActiveAudioInputDevice ? 
                _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)?.DeviceID : 
                string.Empty;
        }

        public string GetCurrentOuputDeviceId()
        {
            return HasActiveAudioOutputDevice ? 
                _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)?.DeviceID : 
                string.Empty;
        }

        private void DefaultDeviceChanged(object _, DefaultDeviceChangedEventArgs args)
        {
            if (args.DataFlow is DataFlow.Capture && args.Role is Role.Multimedia)
            {
                AudioInputDeviceChanged?.Invoke(args.DeviceId);
            }
            else if (args.DataFlow is DataFlow.Render && args.Role is Role.Multimedia)
            {
                AudioOutputDeviceChanged?.Invoke(args.DeviceId);
            }
            else
            {
                UnknownDeviceChanged?.Invoke(args.DeviceId);
            }
        }
    }
}