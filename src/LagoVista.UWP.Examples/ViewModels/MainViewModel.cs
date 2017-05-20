using LagoVista.Core.Commanding;
using LagoVista.Core.IOC;
using LagoVista.Core.Models;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.Networking.Services;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.ViewModels;
using System.Collections.ObjectModel;

namespace LagoVista.UWP.Examples.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        ISSDPClient _client;


        public MainViewModel()
        {
            StartSSDPDiscoveryCommand = new RelayCommand(StartSSDPDiscovery);
            GetSerialPortsCommand = new RelayCommand(GetSerialPorts);
            OpenPortCommand = new RelayCommand(OpenPort);
            OpenPortCommand.Enabled = false;
        }

        public void StartSSDPDiscovery()
        {
            _client = NetworkServices.CreateSSDPClient();
            _client.ShowDiagnostics = true;
            _client.NewDeviceFound += _client_NewDeviceFound;
            _client.SsdpQueryAsync(port: 1900);
        }

        public async void GetSerialPorts()
        {
            var deviceManager = SLWIOC.Get<IDeviceManager>();

            SerialPorts = await deviceManager.GetSerialPortsAsync();
        }

        public async void OpenPort()
        {
            var deviceManager = SLWIOC.Get<IDeviceManager>();
            var port = deviceManager.CreateSerialPort(SelectedSerialPort);
            await port.OpenAsync();
        }


        private void _client_NewDeviceFound(object sender, uPnPDevice e)
        {
            Logger.Log(LogLevel.Message, "ClientFound", e.FriendlyName);
        }

        public RelayCommand StartSSDPDiscoveryCommand { get; private set; }

        public RelayCommand GetSerialPortsCommand { get; private set; }

        public RelayCommand OpenPortCommand { get; private set; }


        ObservableCollection<SerialPortInfo> _serialPorts;
        public ObservableCollection<SerialPortInfo> SerialPorts
        {
            get { return _serialPorts; }
            set { Set(ref _serialPorts, value);  }
        }

        private SerialPortInfo _serialPort;
        public SerialPortInfo SelectedSerialPort
        {
            get { return _serialPort; }
            set
            {
                _serialPort = value;
                OpenPortCommand.Enabled = _serialPort != null;

            }
        }

    }
}
