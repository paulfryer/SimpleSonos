using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscoverTest
{
    public class Room : IRoom
    {
        private readonly List<IDevice> devices = new List<IDevice>();
        private bool? lastMuteValue;
        private int? lastVolumeLevel;
        private string lastPlayingState;

        public string Name { get; set; }
        public void AddDevice(IDevice device)
        {
            device.Muted += OnDeviceMuted;
            device.Unmuted += OnDeviceUnmuted;
            device.VolumeChanged += OnDeviceVolumeChanged;

            device.Stopped += OnStopped;
            device.Transitioning += OnTransitioning;
            device.Playing += OnPlaying;

            devices.Add(device);

            if (DeviceAdded != null)
                DeviceAdded(this, device);
        }

        public List<IDevice> Devices
        {
            get { return devices; }
        }

        public async Task PauseAsync()
        {
            foreach (var device in Devices)
                await device.PauseAsync();
        }

        public async Task PlayAsync()
        {
            foreach (var device in Devices)
                await device.PlayAsync();
        }

        public async Task SetVolumeAsync(int level)
        {
            foreach (var device in Devices)
                await device.SetVolumeAsync(level);
        }





        void OnPlaying(IDevice device)
        {
            if (lastPlayingState == null || lastPlayingState != "PLAYING")
                if (Playing != null)
                    Playing(this);
            lastPlayingState = "PLAYING";
        }

        void OnTransitioning(IDevice device)
        {
            if (lastPlayingState == null || lastPlayingState != "TRANSITIONING")
                if (Transitioning != null)
                    Transitioning(this);
            lastPlayingState = "TRANSITIONING";
        }

        void OnStopped(IDevice device)
        {
            if (lastPlayingState == null || lastPlayingState != "STOPPED")
                if (Stopped != null)
                    Stopped(this);
            lastPlayingState = "STOPPED";
        }

        public string PlayingState
        {
            get { return lastPlayingState; }
        }

        public int? Volume
        {
            get { return lastVolumeLevel; }
        }

        public bool? MuteState
        {
            get { return lastMuteValue; }
        }

        private void OnDeviceVolumeChanged(IDevice device, int level)
        {
            if (!lastVolumeLevel.HasValue || level != lastVolumeLevel.Value)
            {
                if (VolumeChanged != null)
                    VolumeChanged(this, level);
                lastVolumeLevel = level;
            }
        }

        private void OnDeviceMuted(IDevice device)
        {
            if (!lastMuteValue.HasValue || !lastMuteValue.Value)
            {
                if (Muted != null)
                    Muted(this);
                lastMuteValue = true;
            }
        }

        private void OnDeviceUnmuted(IDevice device)
        {
            if (!lastMuteValue.HasValue || lastMuteValue.Value)
            {
                if (Unmuted != null)
                    Unmuted(this);
                lastMuteValue = false;
            }
        }

        public event RoomEventHandler<int> VolumeChanged;
        public event RoomEventHandler<IDevice> DeviceAdded;
        
        public event RoomEventHandler Muted;
        public event RoomEventHandler Unmuted;

        public event RoomEventHandler Stopped;
        public event RoomEventHandler Playing;
        public event RoomEventHandler Transitioning;

    }
}