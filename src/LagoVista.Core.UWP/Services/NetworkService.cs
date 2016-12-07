using LagoVista.Core.PlatformSupport;
using System;
using LagoVista.Core.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LagoVista.Core.Models;

namespace LagoVista.Core.UWP.Services
{
    public class NetworkService : INetworkService
    {
        public event EventHandler NetworkInformationChanged;

        private ObservableCollection<NetworkDetails> _allConnections = new ObservableCollection<NetworkDetails>();

        public ObservableCollection<NetworkDetails> AllConnections { get { return _allConnections; } }

        public NetworkService()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            PopulateNetworks();
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await RefreshAysnc();

            if (NetworkInformationChanged != null)
                NetworkInformationChanged(this, null);
        }

        public bool IsInternetConnected
        {
            get
            {
                var connectionState = NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel();
                return connectionState >= NetworkConnectivityLevel.InternetAccess;
            }
        }


        public string GetIPV4Address()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            var settings = icp.GetNetworkConnectivityLevel();

            if (icp?.NetworkAdapter == null) return null;
            var hostnames =
                NetworkInformation.GetHostNames().Where(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            var ipV4hostName = hostnames.Where(hst => hst.Type == Windows.Networking.HostNameType.Ipv4).FirstOrDefault();


            if (ipV4hostName != null)
                return ipV4hostName.CanonicalName;
            else
                return String.Empty;
        }

        private async void PopulateNetworks()
        {
            await RefreshAysnc();
        }

        public async Task RefreshAysnc()
        {
            var address = GetIPV4Address();
            await Task.Delay(1);
            var profiles = NetworkInformation.GetConnectionProfiles();
            
            var connections = new List<NetworkDetails>();

            var icp = NetworkInformation.GetInternetConnectionProfile();
            var settings = icp.GetNetworkConnectivityLevel();

            if (icp != null && icp.NetworkAdapter != null)
            {
                var hostnames =
                    NetworkInformation.GetHostNames().Where(
                            hn =>
                                hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                                == icp.NetworkAdapter.NetworkAdapterId);

                var ipV4hostName = hostnames.Where(hst => hst.Type == Windows.Networking.HostNameType.Ipv4).FirstOrDefault();
                if(ipV4hostName != null)
                {
                    var network = new NetworkDetails()
                    {
                        Name = icp.ProfileName,
                        IPAddress = ipV4hostName.CanonicalName,

                    };
                    switch (icp.GetNetworkConnectivityLevel())
                    {
                        case NetworkConnectivityLevel.ConstrainedInternetAccess:
                            network.Connectivity = "Limited Internet";
                            break;
                        case NetworkConnectivityLevel.InternetAccess:
                            network.Connectivity = "Internet";
                            break;
                        case NetworkConnectivityLevel.LocalAccess:
                            network.Connectivity = "Local Only";
                            break;
                        case NetworkConnectivityLevel.None:
                            network.Connectivity = "None";
                            break;
                    }

                    connections.Add(network);
                }
            }
  
            LagoVista.Core.PlatformSupport.Services.DispatcherServices.Invoke(() =>
            {
                _allConnections.Clear();
                foreach (var network in connections)
                    _allConnections.Add(network);
            });
        }

        public Task<bool> TestConnectivityAsync()
        {
            throw new NotImplementedException();
        }
    }
}
