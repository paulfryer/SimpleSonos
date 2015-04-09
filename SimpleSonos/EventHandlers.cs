namespace DiscoverTest
{
    public delegate void DeviceEventHandler<in T>(IDevice sonosDevice, T args);

    public delegate void DeviceEventHandler(IDevice sonosDevice);

    public delegate void RoomEventHandler<in T>(IRoom room, T args);

    public delegate void RoomEventHandler(IRoom room);
}