using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace LagoVista.Core.UWP.Services
{
    public class PopupsService : IPopupServices
    {
        public async Task<bool> ConfirmAsync(string title, string prompt)
        {
            try
            {
                var result = false;
                var dlg = new MessageDialog(prompt, title);
                dlg.Commands.Add(new UICommand("Yes") { Invoked = (IUICommand) => { result = true; } });
                dlg.Commands.Add(new UICommand("No") { Invoked = (IUICommand) => { result = false; } });

                await dlg.ShowAsync();
                return result;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public async Task ShowAsync(string message)
        {
            try
            {
                var dlg = new MessageDialog(message);
                await dlg.ShowAsync();
            }
            catch(Exception)
            {

            }
        }

        public async Task ShowAsync(string title, string message)
        {
            try
            {
                var dlg = new MessageDialog(message, title);
                await dlg.ShowAsync();
            }
            catch(Exception)
            {

            }
        }
    }
}
