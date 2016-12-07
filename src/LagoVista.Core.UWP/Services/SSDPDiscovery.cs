﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Networking;
using LagoVista.Core.ServiceCommon;

namespace LagoVista.Core.UWP.Services
{
    public class SSDPDiscoveryConfiguration
    {
        public string DeviceType { get; set; }
        public string FriendlyName { get; set; }
        public string Manufacture { get; set; }
        public string ManufactureUrl { get; set; }
        public string ModelDescription { get; set; }
        public string ModelName { get; set; }
        public string ModelNumber { get; set; }
        public string ModelUrl { get; set; }
        public string SerialNumber { get; set; }
    }

    public class SSDPDiscovery : ServiceBase
    {
        SSDPDiscoveryConfiguration _config;

        private const uint BUFFER_SIZE = 8192;

        private String _udn;
        private int _metaDataPort;

        private readonly DatagramSocket _ssdpDiscoveryListener;
        private readonly StreamSocketListener _webListener;

        private String GetDiscoveryPayload()
        {

            var bldr = new StringBuilder();
            bldr.AppendLine("HTTP/1.1 200 OK");
            bldr.AppendLine(string.Format("ST: urn:schemas-upnp-org:device:{0}:1", _config.DeviceType));
            bldr.AppendLine("CACHE-CONTROL: max-age=90");
            bldr.AppendLine("EXT");
            bldr.AppendLine(String.Format("USN: uuid:{0}::urn:schemas-upnp-org:device:{1}:1", _udn, _config.DeviceType));
            bldr.AppendLine("SERVER: Win10IoT, UPnP/1.0");
            bldr.AppendLine(String.Format("LOCATION: http://{0}:{1}/xml/props.xml", Core.PlatformSupport.Services.Network.GetIPV4Address(), _metaDataPort));
            bldr.AppendLine("");

            return bldr.ToString();
        }

        protected async Task WriteResponseAsync(StreamSocket socket, string contentType, int responseCode, String responseContent)
        {
            var bodyArray = Encoding.ASCII.GetBytes(responseContent);
            using (var resp = socket.OutputStream.AsStreamForWrite())
            using (var stream = new MemoryStream(bodyArray))
            {
                var header = String.Format("HTTP/1.1 {0} OK\r\n" +
                                  "Content-Length: {1}\r\n" +
                                  "Content-Type: {2}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  responseCode, stream.Length, contentType);

                var headerArray = Encoding.UTF8.GetBytes(header);

                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }

        private const string DefaultPage = @"<html>
<head>
<title>Home Health Station Serivces</title>
</head>
<body>
Path not found.
</body>

</html>";

        public SSDPDiscovery()
        {
            _udn = (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["UDN"];
            if (String.IsNullOrEmpty(_udn))
                _udn = Guid.NewGuid().ToString();

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["UDN"] = _udn;

            _ssdpDiscoveryListener = new DatagramSocket();
            _ssdpDiscoveryListener.MessageReceived += _socket_MessageReceived;

            _webListener = new StreamSocketListener();
            _webListener.ConnectionReceived += ProcessRequestAsync;
        }

        private void _ssdpDiscoveryListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
        }

        private async Task SendDeviceInfo(HostName ip, String port, IOutputStream outputStream, DatagramSocket originalSocket)
        {
            LogDetails("Sending SSDP Device Response To: {0}:{1}", ip, port);

            try
            {
                var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(GetDiscoveryPayload(), Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                await outputStream.WriteAsync(buffer);
            }
            catch (Exception ex)
            {
                LogException("SendDeviceInfo", ex);

                if (SocketError.GetStatus(ex.HResult) == SocketErrorStatus.Unknown)
                    LogException("SendDeviceInfo.Hesult", new Exception("Unknown Error" + ex.HResult));
            }
        }


        private async void _socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var result = args.GetDataStream();
            var resultStream = result.AsStreamForRead(1024);

            using (var reader = new StreamReader(resultStream))
            {
                var text = await reader.ReadToEndAsync();

                if (text.StartsWith("M-SEARCH"))
                {
                    var outputStream = await sender.GetOutputStreamAsync(args.RemoteAddress, args.RemotePort);

                    LogDetails(text);
                    await SendDeviceInfo(args.RemoteAddress, args.RemotePort, outputStream, sender);
                }

            }
        }


        public async void MakeDiscoverable(int metaDataPort, SSDPDiscoveryConfiguration config)
        {
            _config = config;

            _metaDataPort = metaDataPort;
            await _webListener.BindServiceNameAsync(metaDataPort.ToString());
            await _ssdpDiscoveryListener.BindEndpointAsync(null, "1900");
            _ssdpDiscoveryListener.JoinMulticastGroup(new HostName("239.255.255.250"));
        }

        private async void ProcessRequestAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var socket = args.Socket;

            try
            {
                var request = new StringBuilder();
                using (var input = socket.InputStream)
                {
                    var data = new byte[BUFFER_SIZE];
                    var buffer = data.AsBuffer();
                    var dataRead = BUFFER_SIZE;
                    while (dataRead == BUFFER_SIZE)
                    {
                        await input.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (var output = socket.OutputStream)
                {
                    Debug.WriteLine(request);

                    if (request.ToString().ToLower().Contains("favicon"))
                        await WriteResponseAsync(socket, "text", 404, "NOT FOUND");
                    else
                    {
                        var requestMethod = request.ToString().Split('\n')[0];
                        var requestParts = requestMethod.Split(' ');

                        await Process(socket, requestParts[0], requestParts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException("ProcessRequestAsync", ex);
            }
        }

        private async Task Process(StreamSocket socket, String method, String path)
        {
            if (path.ToLower() == "/xml/props.xml")
            {
                await WriteResponseAsync(socket, "application/xml", 200, GetDeviceProps());
                return;
            }

            var rootDirectory = path.ToLower().Split('/');
            if(rootDirectory.Length == 0)
            {
                await WriteResponseAsync(socket, "text/html", 200, DefaultPage);
                return;
            }

            if (!await HandleRequestAsync(socket, path))
                await WriteResponseAsync(socket, "text/html", 200, DefaultPage);
        }

        public virtual async Task<bool> HandleRequestAsync(StreamSocket socket, string path)
        {
            await WriteResponseAsync(socket, "text/html", 200, DefaultPage);
            return true;
        }

        private String GetDeviceProps()
        {
            String _deviceXML =
          @"<?xml version=""1.0""?>
<root xmlns=""urn:schemas-upnp-org:device-1-0"" >
    <specVersion >
    <major> 1 </major>
    <minor> 0 </minor>
    </specVersion >
 <device>
     <deviceType>urn:schemas-upnp-org:device:" + _config.DeviceType + @":1</deviceType>
     <presentationURL>/</presentationURL>
     <friendlyName>" + _config.FriendlyName + @"</friendlyName>
     <manufacturer>" + _config.Manufacture + @"</manufacturer>
     <manufacturerURL>" + _config.ManufactureUrl + @"</manufacturerURL>
     <modelDescription>" + _config.ModelDescription + @"</modelDescription>
     <modelName>" + _config.ModelName + @"</modelName>
     <modelNumber>" + _config.ModelNumber + @"</modelNumber>
     <modelURL>" + _config.ModelUrl + @"</modelURL>
     <serialNumber>" + _config.SerialNumber + @"</serialNumber>
     <UDN>uuid:" + _udn + @"</UDN>
     <serviceList>
        <service>
            <serviceType>urn:schemas-upnp-org:service:Dimming:1</serviceType>
            <serviceId>urn:upnp-org:serviceId:Dimming.0001</serviceId>
            <SCPDURL>_urn-upnp-org-serviceId-Dimming.0001_scpd.xml</SCPDURL>
            <controlURL>_urn-upnp-org-serviceId-Dimming.0001_control</controlURL>
            <eventSubURL>_urn-upnp-org-serviceId-Dimming.0001_event</eventSubURL>
        </service>
        <service>
            <serviceType>urn:schemas-upnp-org:service:SwitchPower:1</serviceType>
            <serviceId>urn:upnp-org:serviceId:SwitchPower.0001</serviceId>
            <SCPDURL>_urn-upnp-org-serviceId-SwitchPower.0001_scpd.xml</SCPDURL>
            <controlURL>_urn-upnp-org-serviceId-SwitchPower.0001_control</controlURL>
            <eventSubURL>_urn-upnp-org-serviceId-SwitchPower.0001_event</eventSubURL>
        </service>
    </serviceList>
 </device>
</root>";

            return _deviceXML;
        }
    }
}
