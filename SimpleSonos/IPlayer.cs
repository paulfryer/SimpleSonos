using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SimpleSonos
{
    public interface IPlayer
    {
        Task PlayAsync();
        Task PauseAsync();
        Task SetVolumeAsync(int level);
    }

    public interface IDevice : IPlayer
    {
        IPAddress IpAddress { get; set; }
        event DeviceEventHandler<int> VolumeChanged;
        event DeviceEventHandler Muted;
        event DeviceEventHandler Unmuted;
        event DeviceEventHandler Playing;
        event DeviceEventHandler Stopped;
        event DeviceEventHandler Transitioning;
        event DeviceEventHandler<MusicTrack> TrackUpdate;
        void RecordStateVariableChange(string xml);
    }

    public interface IRoom : IPlayer
    {
        string PlayingState { get; }
        int? Volume { get; }
        bool? MuteState { get; }
        string Name { get; }
        void AddDevice(IDevice device);
        List<IDevice> Devices { get; }
        event RoomEventHandler<int> VolumeChanged;
        event RoomEventHandler<IDevice> DeviceAdded;
        event RoomEventHandler Muted;
        event RoomEventHandler Unmuted;
        event RoomEventHandler Stopped;
        event RoomEventHandler Playing;
        event RoomEventHandler Transitioning;
    }
}