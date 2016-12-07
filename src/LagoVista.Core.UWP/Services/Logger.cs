using LagoVista.Core.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Core.UWP.Services
{
    public class Logger : ILogger
    {
        private String _userId;

        public void SetKeys(params string[] args)
        {

        }


        public void Log(LogLevel level, string area, string message, params KeyValuePair<string, string>[] args)
        {
            Debug.WriteLine("AREA       : " + area);
            Debug.WriteLine("Message       : " + message);
        }

        public void LogException(string area, Exception ex, params KeyValuePair<string, string>[] args)
        {
            Debug.WriteLine("AREA       : " + area);
            Debug.WriteLine("Exception  : " + ex.Message);
            if (!String.IsNullOrEmpty(ex.StackTrace))
                Debug.WriteLine(ex.StackTrace);
            else
                Debug.Write("NO STACK TRACE");

        }

        public void SetUserId(string userId)
        {
            _userId = userId;
        }

        public void TrackEvent(string message, Dictionary<string, string> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
