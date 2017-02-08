using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using LagoVista.Core.ServiceCommon;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.Networking.Interfaces;

namespace LagoVista.Core.UWP.Services
{
    public class WebServer : ServiceBase, IDisposable
    {
        private const uint BUFFER_SIZE = 16384;
        private readonly StreamSocketListener _streamSocketListener;

        public event EventHandler<HttpRequestMessage> MessageReceived;

        RestMessageHandler _messageHandler;

        public WebServer()
        {
            _streamSocketListener = new StreamSocketListener();
            _streamSocketListener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);

            _messageHandler = new RestMessageHandler();
        }

        public int Port { get; private set; }


        public void StartServer(int port)
        {
            Port = port;
#pragma warning disable CS4014
            _streamSocketListener.BindServiceNameAsync(Port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            _streamSocketListener.Dispose();
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            var request = new StringBuilder();
            using (var input = socket.InputStream)
            using (var rdr = new StreamReader(input.AsStreamForRead()))
            {
                var data = new byte[BUFFER_SIZE];
                var buffer = data.AsBuffer();
                var dataRead = BUFFER_SIZE;
                while (dataRead == BUFFER_SIZE)
                {
                    var readBuffer = await input.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, (int)BUFFER_SIZE));
                    dataRead = buffer.Length;
                }
            }

            if (request.Length > 0)
            {
                var message = HttpRequestMessage.Create(socket.OutputStream.AsStreamForWrite(), request.ToString());
                LogDetails("Message Received\r\n" + message.ToString());

                if (MessageReceived != null)
                    MessageReceived(this, message);
                else
                {
                    if (!await OnMessageArrived(message))
                    {
                        var response = message.GetResponseMessage();
                        response.ContentType = "text/html";
                        response.Content = "<html><head><title>LagoVista IoT Home Automation and Devices Framewroks</title></head><body>Web Server Not Implemented</body></html>";
                        await response.Send();
                    }
                }
            }
        }


        protected virtual Task<bool> OnMessageArrived(HttpRequestMessage msg)
        {
            return _messageHandler.HandleMessage(msg);
        }

        private ILogger _logger;
        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                _logger = value;
                _messageHandler.Logger = value;
            }
        }

        public void RegisterAPIHandler(IApiHandler handler)
        {
            _messageHandler.Handlers.Add(handler);
        }
    }
}