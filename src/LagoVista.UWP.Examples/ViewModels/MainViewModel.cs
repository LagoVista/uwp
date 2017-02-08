using LagoVista.Core.Commanding;
using LagoVista.Core.IOC;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.ViewModels;


namespace LagoVista.UWP.Examples.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        ISSDPClient _client;


        public MainViewModel()
        {
            StartSSDPDiscoveryCommand = new RelayCommand(StartSSDPDiscovery);
        }

        public void StartSSDPDiscovery()
        {
            var logger = SLWIOC.Get<ILogger>();
            _client = SLWIOC.Get<ISSDPClient>();
            _client.ShowDiagnostics = true;
            _client.NewDeviceFound += _client_NewDeviceFound;
            _client.SsdpQueryAsync(port: 1901);
        }


        private void _client_NewDeviceFound(object sender, uPnPDevice e)
        {
            Logger.Log(LogLevel.Message, "ClientFound", e.FriendlyName);
        }

        public RelayCommand StartSSDPDiscoveryCommand { get; private set; }

    }
}
