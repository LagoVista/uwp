using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using System.Diagnostics;
using System.IO;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Models;
using LagoVista.Core.ServiceCommon;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.Networking.Models;

namespace LagoVista.Core.UWP.Services
{
    public class SSDPClient : ServiceBase, ISSDPClient
    {
        DatagramSocket _readerSocket;

        const string multicastIP = "239.255.255.250";
        const int multicastPort = 1900;
        //const int unicastPort = 1901fdfa
        const int MaxResultSize = 8000;

        public event EventHandler<uPnPDevice> NewDeviceFound;

        private List<String> _foundDevices = new List<string>();

        private const string SSDP_IP = "239.255.255.250";
        private const string SSDP_PORT = "1900";
        private const string SSDP_QUERY = "M-SEARCH * HTTP/1.1\r\n" +
                                          "HOST: " + SSDP_IP + ":" + SSDP_PORT + "\r\n" +
                                          "MAN: \"ssdp:discover\"\r\n" +
                                          "MX: [MAX_TIME]\r\n" +
                                          "ST: [SDPFILTERTYPE]\r\n\r\n";

        private string ProcessSSDPResponse(string response)
        {
            var reader = new StringReader(response);
            var lines = new List<string>();
            string line;
            for (;;)
            {
                line = reader.ReadLine();

                if (line == null)
                    break;

                if (line != "")
                    lines.Add(line);
            }

            var location = lines.Where(lin => lin.ToLower().StartsWith("location:")).FirstOrDefault();

            if (!_foundDevices.Contains(location.ToLower()))
            {
                _foundDevices.Add(location.ToLower());
                return location;
            }
            else
                return null;
        }

        public ILogger Logger
        {
            get; set;
        }

        /* Handle the SSDP Header, primary task is to download location of the XML document used to get more info */
        private void HandleSSDPResponse(string result)
        {
            Debug.WriteLine(result);
            var newservice = ProcessSSDPResponse(result);
            if (newservice != null)
            {
                var startIdx = newservice.IndexOf(':');
                var uri = newservice.Substring(startIdx + 1);

                uPnPDevice.GetDetails(uri.Trim(), (device) =>
                {
                    if (device != null)
                    {
                        LogDetails(device.ToString());

                        if (NewDeviceFound != null)
                            NewDeviceFound(this, device);
                    }
                });
            }
        }

        public async Task SsdpQueryAsync(string filter = "ssdp:all", int seconds = 5)
        {
            _foundDevices = new List<string>();
            var remoteIP = new Windows.Networking.HostName(SSDP_IP);
            var ssdpQuery = SSDP_QUERY.Replace("[SDPFILTERTYPE]", filter).Replace("[MAX_TIME]", seconds.ToString());
            var reqBuff = Encoding.UTF8.GetBytes(ssdpQuery);

            try
            {
                if (_readerSocket != null)
                    _readerSocket.Dispose();
            }
            catch(Exception ex)
            {
                LogException("SSDPFinder.QueryAsync - Close Existing Socket", ex);
            }

            _readerSocket = new DatagramSocket();
            _readerSocket.MessageReceived += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    var addr = args.RemoteAddress.DisplayName;
                    var result = args.GetDataStream();
                    var resultStream = result.AsStreamForRead(1024);

                    using (var reader = new StreamReader(resultStream))
                    {
                        var resultText = await reader.ReadToEndAsync();

                        if (resultText.StartsWith("HTTP/1.1 200 OK"))
                            HandleSSDPResponse(resultText);
                    };
                });
            };

            await _readerSocket.BindEndpointAsync(null, "");
            Core.PlatformSupport.Services.Logger.Log(LogLevel.Message, "SSDPFinder.QueryAsync", _readerSocket.Information.LocalAddress + " " + _readerSocket.Information.LocalPort);

            _readerSocket.JoinMulticastGroup(remoteIP);

            using (var stream = await _readerSocket.GetOutputStreamAsync(remoteIP, SSDP_PORT))
            {
                await stream.WriteAsync(reqBuff.AsBuffer());
            }
        }

        public void Cancel()
        {
            if (_readerSocket != null)
            {
                _readerSocket.Dispose();
                _readerSocket = null;
            }
        }

    }
}
