using LagoVista.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Reflection;
using Windows.UI.Xaml.Controls;

namespace LagoVista.UWP.UI
{
    public class Navigation : IViewModelNavigation
    {
        private static Navigation _instance = new Navigation();
        private Frame _rootFrame;

        private IDictionary<Type, Type> _navDictionary = new Dictionary<Type, Type>();

        public void Initialize(Frame rootFrame)
        {
            _rootFrame = rootFrame;
            SystemNavigationManager.GetForCurrentView().BackRequested += Navigation_BackRequested;
        }

        private void Navigation_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_rootFrame.CanGoBack)
                _rootFrame.GoBack();

            e.Handled = true;
        }

        public void Navigate(ViewModelLaunchArgs args)
        {
            Type viewType = null;
            if (_navDictionary.Keys.Contains(args.ViewModelType))
                viewType = _navDictionary[args.ViewModelType];
            else
            {
                Type viewModelInterface = args.ViewModelType;
                var key = _navDictionary.Keys.Where(cls => cls.GetInterfaces().Contains(args.ViewModelType)).FirstOrDefault();
                if (key == null)
                    throw new Exception(String.Format("Could not find view for {0}", args.ViewModelType.FullName));
                args.ViewModelType = key;
                viewType = _navDictionary[key];
            }

            if (args.Parameter != null)
                _rootFrame.Navigate(viewType, args);
            else
                _rootFrame.Navigate(viewType, args);
        }

        public static Navigation Instance { get { return _instance; } }

        public void Add<T, V>() where T : ViewModelBase where V : LagoVistaPage
        {
            _navDictionary.Add(typeof(T), typeof(V));
        }

        public bool CanGoBack()
        {
            return _rootFrame.CanGoBack;
        }

        public void GoBack()
        {
            if (_rootFrame.CanGoBack)
                _rootFrame.GoBack();
        }

        public void Navigate<T>() where T : ViewModelBase
        {
            Navigate(new ViewModelLaunchArgs()
            {
                ViewModelType = typeof(T)
            });
        }

        public void PopToRoot()
        {
            throw new NotImplementedException();
        }

        public void SetAsNewRoot()
        {
            throw new NotImplementedException();
        }

        public void SetAsNewRoot<TViewModel>() where TViewModel : ViewModelBase
        {
            throw new NotImplementedException();
        }
    }
}
