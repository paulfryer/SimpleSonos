using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DiscoverTest
{
    public class Device : IDevice
    {
        public IPAddress IpAddress { get; set; }

        public string RoomName { get; set; }

        private Uri MakeUri(string relativeUrl)
        {
            return new Uri("http://" + IpAddress + ":1400" + relativeUrl);
        }


        public event DeviceEventHandler<int> VolumeChanged;

        public event DeviceEventHandler Muted;
        public event DeviceEventHandler Unmuted;

        public event DeviceEventHandler Playing;
        public event DeviceEventHandler Stopped;
        public event DeviceEventHandler Transitioning;
        public event DeviceEventHandler<MusicTrack> TrackUpdate;

        public void RecordStateVariableChange(string xml)
        {
      
            var doc = XElement.Parse(xml);

            var currentTrackMetaDataNode = doc
                .Descendants(XName.Get("CurrentTrackMetaData", "urn:schemas-upnp-org:metadata-1-0/AVT/"))
                .SingleOrDefault(n => !String.IsNullOrEmpty(n.Attribute("val").Value));

            var transportStateNode = doc
                .Descendants(XName.Get("TransportState", "urn:schemas-upnp-org:metadata-1-0/AVT/"))
                .SingleOrDefault(n => n.HasAttributes);

            var masterMuteNode = doc
                .Descendants(XName.Get("Mute", "urn:schemas-upnp-org:metadata-1-0/RCS/"))
                .SingleOrDefault(n => n.Attribute("channel").Value == "Master");


            var masterVolumeNode = doc
                .Descendants(XName.Get("Volume", "urn:schemas-upnp-org:metadata-1-0/RCS/"))
                .SingleOrDefault(n => n.Attribute("channel").Value == "Master");

            if (currentTrackMetaDataNode != null)
            {
                var xmlString = currentTrackMetaDataNode.Attribute("val").Value;
                var track = MusicTrack.Parse(xmlString);

                if (track != null)
                {
                    RecordTrackUpdated(track);
                }
            }


            if (transportStateNode != null)
            {
                var transportState = transportStateNode.Attribute("val").Value;
                switch (transportState)
                {
                    case "STOPPED":
                    case "PAUSED_PLAYBACK":
                        RecordStopped();
                        break;
                    case "TRANSITIONING":
                        RecordTransitioning();
                        break;
                    case "PLAYING":
                        RecordPlaying();
                        break;
                }
            }

            if (masterVolumeNode != null)
            {
                var volume = int.Parse(masterVolumeNode.Attribute("val").Value);
                RecordVolumeChange(volume);
            }

            if (masterMuteNode != null)
            {
                var muted = Convert.ToBoolean(int.Parse(masterMuteNode.Attribute("val").Value));
                RecordMuteChange(muted);
            }
        }

        public void RecordTrackUpdated(MusicTrack track)
        {
            if (TrackUpdate != null)
                TrackUpdate(this, track);
        }

        public void RecordVolumeChange(int level)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, level);
        }

        public void RecordMuteChange(bool muted)
        {
            if (muted && Muted != null)
                Muted(this);
            if (!muted && Unmuted != null)
                Unmuted(this);
        }

        public void RecordPlayed()
        {
            if (Playing != null)
                Playing(this);
        }


        public async Task PlayAsync()
        {
            const string xml =
               "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:Play xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Speed>1</Speed></u:Play></s:Body></s:Envelope>";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("USER-AGENT",
                "Linux UPnP/1.0 Sonos/28.1-86200 (WDCR:Microsoft Windows NT 6.2.9200.0)");
            var request = new HttpRequestMessage(HttpMethod.Post, MakeUri(Endpoints.Control.AvTransport))
            {
                Content = new StringContent(xml)
            };
            request.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            await httpClient.SendAsync(request);
        }

        public async Task PauseAsync()
        {
            const string xml =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:Pause xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID></u:Pause></s:Body></s:Envelope>";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("USER-AGENT",
                "Linux UPnP/1.0 Sonos/28.1-86200 (WDCR:Microsoft Windows NT 6.2.9200.0)");
            var request = new HttpRequestMessage(HttpMethod.Post, MakeUri(Endpoints.Control.AvTransport))
            {
                Content = new StringContent(xml)
            };
            request.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:AVTransport:1#Pause\"");
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            await httpClient.SendAsync(request);
        }



        public async Task SetVolumeAsync(int level)
        {
            var xml =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:SetVolume xmlns:u=\"urn:schemas-upnp-org:service:RenderingControl:1\"><InstanceID>0</InstanceID><Channel>Master</Channel><DesiredVolume>" +
                level + "</DesiredVolume></u:SetVolume></s:Body></s:Envelope>";

           // using (var wc = new WebClient())
           // {
           //     wc.Headers.Add("SOAPACTION", "urn:schemas-upnp-org:service:RenderingControl:1#SetVolume");
           //     await wc.UploadStringAsync(MakeUri(Endpoints.Control.RenderingControl), xml);
           // }

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, MakeUri(Endpoints.Control.RenderingControl))
            {
                Content = new StringContent(xml)
            };
            request.Headers.Add("SOAPACTION", "urn:schemas-upnp-org:service:RenderingControl:1#SetVolume");
            await httpClient.SendAsync(request);
        }
        
        public static class Endpoints
        {
            public static class Control
            {
                public static string RenderingControl
                {
                    get { return "/MediaRenderer/RenderingControl/Control"; }
                }

                public static string AvTransport
                {
                    get { return "/MediaRenderer/AVTransport/Control"; }
                }
            }

            public static class Event
            {
                public static string ZoneGroupTopology
                {
                    get { return "/ZoneGroupTopology/Event"; }
                }


                public static string SystemProperties
                {
                    get { return "/SystemProperties/Event"; }
                }

                public static string MusicServices
                {
                    get { return "/MusicServices/Event"; }
                }

                public static string AlarmClock
                {
                    get { return "/AlarmClock/Event"; }
                }

                public static class MediaRenderer
                {
                    public static string RenderingControl
                    {
                        get { return "/MediaRenderer/RenderingControl/Event"; }
                    }
                }

                public static class MediaServer
                {
                    public static string ContentDirectory
                    {
                        get { return "/MediaServer/ContentDirectory/Event"; }
                    }
                }
            }
        }

        public void RecordStopped()
        {
            if (Stopped != null)
                Stopped(this);
        }

        public void RecordTransitioning()
        {
            if (Transitioning != null)
                Transitioning(this);
        }

        public void RecordPlaying()
        {
            if (Playing != null)
                Playing(this);
        }

    }
}