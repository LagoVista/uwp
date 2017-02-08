using LagoVista.Core.Commanding;
using LagoVista.Core.IOC;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.UWP.Examples.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        ISSDPServer _server;

        UPNPConfiguration _configuration = new UPNPConfiguration()
        {
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
        }

        public void StartSSDPServer()
        {
            var logger = SLWIOC.Get<ILogger>();
            _server = SLWIOC.Get<ISSDPServer>();

            //_server = Core.Networking.Services.NetworkServices.GetSSDPServer();
            _server.MakeDiscoverable(9050, _configuration);
            _server.ShowDiagnostics = true;
        }

        public RelayCommand StartSSDPServerCommand { get; private set; }
    }
}
