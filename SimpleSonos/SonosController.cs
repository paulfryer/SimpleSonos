using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using ManagedUPnP;
using ManagedUPnP.Components;
using Device = SimpleSonos.Device;

namespace SimpleSonos
{
    public class SonosController
    {
        private readonly UPnPDiscovery upnp = new UPnPDiscovery();
        public List<IRoom> Rooms = new List<IRoom>();
        public TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

        public SonosController()
        {
            upnp.SearchStarted += UpnpOnSearchStarted;
            upnp.SearchEnded += UpnpOnSearchEnded;
            upnp.ServiceAdded += OnServiceAdded;
        }


        public event EventHandler IndexingStarted;
        public event EventHandler IndexingEnded;
        //public event EventHandler<Device> DeviceFound;

        public event RoomEventHandler RoomFound;

        private void UpnpOnSearchEnded(object sender, EventArgs eventArgs)
        {
            if (IndexingEnded != null)
                IndexingEnded(this, null);
        }

        private void UpnpOnSearchStarted(object sender, EventArgs eventArgs)
        {
            if (IndexingStarted != null)
                IndexingStarted(this, null);
        }

        private static string GetRoomName(Service service)
        {
            var friendlyName = service.Device.FriendlyName.Replace(" Media Renderer", string.Empty);
            friendlyName = friendlyName.Replace(" - ", ":");
            return friendlyName.Split(':')[0];
        }

        private static string GetSonosDeviceTypeName(Service service)
        {
            var friendlyName = service.Device.FriendlyName.Replace(" Media Renderer", string.Empty);
            friendlyName = friendlyName.Replace(" - ", ":");
            return friendlyName.Split(':')[1];
        }

        private Device GetSonosDevice(Service service)
        {
            var roomName = GetRoomName(service);
            var sonosTypeName = GetSonosDeviceTypeName(service);

            var device = new Device
            {
                DeviceTypeCode = sonosTypeName
            };

            var ipString = service.Device.DocumentURL
                .Replace("http://", string.Empty)
                .Replace(":1400/xml/device_description.xml", string.Empty);

            device.IpAddress = IPAddress.Parse(ipString);
            device.RoomName = roomName;

            return device;
        }


        private void OnServiceAdded(object sender, ServiceAddedEventArgs e)
        {
            if (e.Service.Device.ManufacturerName == "Sonos, Inc.")
            {
                e.Service.StateVariableChanged += ServiceOnStateVariableChanged;

                if (e.Service.ServiceTypeIdentifier == "urn:schemas-upnp-org:service:RenderingControl:1")
                {
                    var device = GetSonosDevice(e.Service);

                    var room = Rooms.SingleOrDefault(r => r.Name == device.RoomName);

                    if (room == null)
                    {
                        var newRoom = new Room
                        {
                            Name = device.RoomName
                        };
                        Rooms.Add(newRoom);
                        if (RoomFound != null)
                            RoomFound(newRoom);
                        newRoom.AddDevice(device);
                    }
                    else
                        room.AddDevice(device);
                }
            }
        }

        private IDevice GetDeviceByIpAddress(IPAddress ipAddress)
        {
            foreach (var room in Rooms)
                foreach (var d in room.Devices)
                    if (d.IpAddress.ToString() == ipAddress.ToString())
                        return d;
            return null;
        }

        private void ServiceOnStateVariableChanged(object sender, StateVariableChangedEventArgs args)
        {
            //args.StateVarName;

            var service = sender as Service;
            var deviceIpAddress = GetSonosDevice(service).IpAddress;
            var device = GetDeviceByIpAddress(deviceIpAddress);
            if (device != null && args.StateVarName == "LastChange")
                device.RecordStateVariableChange((string)args.StateVarValue);
        }

        public void IndexRooms()
        {
            //var x = upnp.ResolveNetworkInterface;

            upnp.Active = true;
            Thread.Sleep(SearchTimeout);
            upnp.Active = false;
        }
    }

    public class MusicTrack
    {
        public string Title { get; set; }
        public string Creator { get; set; }
        public Album Album { get; set; }
        public Uri ResourceUri { get; set; }

        private static string SetValueIfNodeNotNull(XElement element, string targetElementName,
            string targetElementNamespace)
        {
            var node = element.Descendants(XName.Get(targetElementName, targetElementNamespace)).SingleOrDefault();
            return node != null ? node.Value : null;
        }

        public static MusicTrack Parse(string xml)
        {
            var track = new MusicTrack();

            var element = XElement.Parse(xml);

            var classType = SetValueIfNodeNotNull(element, "class", "urn:schemas-upnp-org:metadata-1-0/upnp/");
            if (classType != "object.item.audioItem.musicTrack")
                return null;

            track.Title = SetValueIfNodeNotNull(element, "title", "http://purl.org/dc/elements/1.1/");
            track.Creator = SetValueIfNodeNotNull(element, "creator", "http://purl.org/dc/elements/1.1/");

            var resourceUri = SetValueIfNodeNotNull(element, "res", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/");
            
            if (!string.IsNullOrEmpty(resourceUri))
                track.ResourceUri = new Uri(resourceUri);

          
            track.Album = new Album
            {
                Name = SetValueIfNodeNotNull(element, "album", "urn:schemas-upnp-org:metadata-1-0/upnp/")
            };

              var albumArtUri = SetValueIfNodeNotNull(element, "albumArtURI", "urn:schemas-upnp-org:metadata-1-0/upnp/");

            if (!string.IsNullOrEmpty(albumArtUri))
                track.Album.ImageUrl = new Uri(albumArtUri);

            return track;
        }
    }

    public class Album
    {
        public Uri ImageUrl { get; set; }
        public string Name { get; set; }
    }
}