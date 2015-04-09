using System;
using System.Threading;
using System.Threading.Tasks;
using DiscoverTest;

namespace SimpleSonos.ConsoleTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sonosController = new SonosController();

            sonosController.IndexingStarted += OnIndexingStarted;
            //sonosController.DeviceFound += OnDeviceFound;
            sonosController.IndexingEnded += OnIndexingEnded;
            sonosController.RoomFound += sonosController_RoomFound;

            sonosController.IndexRooms();

            DoWork(sonosController);

            var key = Console.ReadKey();
        }

        private static async Task DoWork(SonosController sonosController)
        {
            foreach (var room in sonosController.Rooms)
            {
                if (room.Name == "Office")
                {
                    await room.SetVolumeAsync(40);
                    //while (true)
                    //{
                    await room.PauseAsync();
                    Thread.Sleep(1000);
                    await room.PlayAsync();
                    Thread.Sleep(1000);
                    //}
                }
            }
        }


        private static void sonosController_RoomFound(IRoom room)
        {
            room.DeviceAdded += room_DeviceAdded;
            room.VolumeChanged += room_VolumeChanged;
            room.Muted += room_Muted;
            room.Unmuted += room_Unmuted;

            room.Stopped += room_Stopped;
            room.Transitioning += room_Transitioning;
            room.Playing += room_Playing;


            Console.WriteLine("ROOM FOUND: " + room.Name);
        }

        private static void room_Playing(IRoom room)
        {
            Console.WriteLine("ROOM PLAYING: " + room.Name);
        }

        private static void room_Transitioning(IRoom room)
        {
            Console.WriteLine("ROOM TRANSITIONING: " + room.Name);
        }

        private static void room_Stopped(IRoom room)
        {
            Console.WriteLine("ROOM STOPPED: " + room.Name);
        }

        private static void room_Unmuted(IRoom room)
        {
            Console.WriteLine("ROOM Unmuted: " + room.Name);
        }

        private static void room_Muted(IRoom room)
        {
            Console.WriteLine("ROOM Muted: " + room.Name);
        }

        private static void room_VolumeChanged(IRoom room, int level)
        {
            Console.WriteLine("VOLUME CHANGE ROOM: " + room.Name + ", LEVEL: " + level);
        }

        private static void room_DeviceAdded(IRoom room, IDevice device)
        {
            device.TrackUpdate += device_TrackUpdate;
            // device.VolumeChanged += OnDeviceVolumeChanged;
            //device.Muted += OnDeviceMuted;
            //device.Unmuted += OnDeviceUnmuted;
            //Console.WriteLine("DEVICE FOUND. Room: " + room.Name + ", Type: " + device.GetType().Name);
        }

        private static void device_TrackUpdate(IDevice sonosDevice, MusicTrack args)
        {
            Console.WriteLine("Device: " + sonosDevice.IpAddress + ", Track Update: " + args.Title + ", Album: " +
                              args.Album.Name + ", Artist: " + args.Creator);
        }


        private static void OnIndexingStarted(object s, EventArgs a)
        {
            Console.WriteLine("Indexing Started");
        }

        private static void OnIndexingEnded(object s, EventArgs a)
        {
            Console.WriteLine("Finished Indexing");
        }

        private static void OnDeviceVolumeChanged(Device device, int args)
        {
            Console.WriteLine(device.IpAddress + " Volume Changed: " + args);
        }

        private static void OnDeviceUnmuted(Device device)
        {
            Console.WriteLine("Device Unmuted, " + device.IpAddress);
        }

        private static void OnDeviceMuted(Device device)
        {
            Console.WriteLine("Device Muted, " + device.IpAddress);
        }
    }
}