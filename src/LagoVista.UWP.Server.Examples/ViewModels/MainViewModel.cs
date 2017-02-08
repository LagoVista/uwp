using LagoVista.Core.Commanding;
using LagoVista.Core.IOC;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.Networking.Services;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.UWP.Server.Examples.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        ISSDPServer _server;
        IWebServer _webServer;

        UPNPConfiguration _configuration = new UPNPConfiguration()
        {
            UdpListnerPort = 1901,
            FriendlyName = "Example uPnP Server",
            Manufacture = "Software Logistics, LLC",
            SerialNumber = "1234",
            DefaultPageHtml = @"<html>
<head>
<title>Lago Vista uPnP server Sample App</title>
</head>
<body>
Path not found.
</body>

</html>"
        };


        public MainViewModel()
        {
            StartSSDPServerCommand = new RelayCommand(StartSSDPServer);
            StartAPIServerCommand = new RelayCommand(StartAPIServer);
        }

        public void StartSSDPServer()
        {
            _server = SLWIOC.Get<ISSDPServer>();

            //_server = Core.Networking.Services.NetworkServices.GetSSDPServer();
            _server.MakeDiscoverable(9050, _configuration);
            _server.ShowDiagnostics = true;
        }

        public void StartAPIServer()
        {
            //_webServer = NetworkServices.GetWebServer();
            _webServer = SLWIOC.Create<IWebServer>();
            _webServer.RegisterAPIHandler(new HandlerOne());
            _webServer.StartServer(80);

        }

        public RelayCommand StartSSDPServerCommand { get; private set; }

        public RelayCommand StartAPIServerCommand { get; private set; }
    }

    public class HandlerOne : IApiHandler
    {
        public HttpResponseMessage Add(HttpRequestMessage msg, int numberOne, int numberTwo)
        {
            var responseMessage = msg.GetResponseMessage();

            responseMessage.Content  = (numberOne + numberTwo).ToString();

            return responseMessage;
        }

        [MethodHandler(MethodHandlerAttribute.MethodTypes.GET)]
        public HttpResponseMessage HandleOptions(HttpRequestMessage msg)
        {
            var responseMessage = msg.GetResponseMessage();
            responseMessage.Content = "HELLO WORLD";
            return responseMessage;
        }

        [MethodHandler(MethodHandlerAttribute.MethodTypes.GET, FullPath = "/Options/Handle/foo/{param1}/{param2}/{param3}/{param4}")]
        public HttpResponseMessage DoSomething(HttpRequestMessage msg, String param1, System.Int32 param2, int param3, Guid param4)
        {
            Debug.WriteLine(String.Format("{0} - {1} - {2} - {3}", param1, param2, param3, param4));

            var responseMessage = msg.GetResponseMessage<SampleData>();
            responseMessage.Payload = new SampleData()
            {
                Fee = "Kevin",
                Foo = "TEsting",
            };

            return responseMessage;
        }

        [MethodHandler(MethodHandlerAttribute.MethodTypes.POST, FullPath = "/Options/Handle/foo/{param1}")]
        public HttpResponseMessage DidSomething(HttpRequestMessage msg, Guid param1, SampleData obj)
        {           
            Debug.WriteLine(obj.Fee + " " + obj.Foo);

            var responseMessage = msg.GetResponseMessage();

            return responseMessage;
        }
    }

    public class SampleData
    {
        public string Fee { get; set; }
        public string Foo { get; set; }
    }
}
