using LagoVista.Core;
using LagoVista.Core.IOC;
using LagoVista.Core.Networking.Interfaces;
using LagoVista.Core.PlatformSupport;
using Windows.UI.Core;

namespace LagoVista.Core.UWP.Services
{
    public static class UWPDeviceServices
    {
        public static void Init(CoreDispatcher dispatcher)
        {
            SLWIOC.Register<IDispatcherServices>(new DispatcherServices(dispatcher));
            SLWIOC.Register<IStorageService>(new StorageService());
            SLWIOC.Register<IPopupServices>(new PopupsService());
            SLWIOC.Register<ILogger>(new Logger());
            SLWIOC.Register<INetworkService>(new NetworkService());
            SLWIOC.Register<IImaging>(new Imaging());
            SLWIOC.Register<IBindingHelper>(new BindingHelper());
            SLWIOC.Register<ISSDPClient>(new SSDPClient());
            SLWIOC.Register<IWebServer>(new WebServer());
            SLWIOC.Register<ISSDPClient>(typeof(SSDPClient));
            SLWIOC.Register<IWebServer>(typeof(WebServer));
            SLWIOC.Register<ISSDPServer>(new SSDPServer());
            SLWIOC.Register<ITimerFactory>(new TimerFactory());
            SLWIOC.Register<IDirectoryServices>(new DirectoryServices());
        }
    }
}
