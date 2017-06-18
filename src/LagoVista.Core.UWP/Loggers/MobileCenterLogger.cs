using LagoVista.Core.PlatformSupport;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using System;
using System.Collections.Generic;

namespace LagoVista.Core.UWP.Loggers
{
    public class MobileCenterLogger : ILogger
    {
        private String _userId;

        KeyValuePair<String, String>[] _args;

        public MobileCenterLogger(string key)
        {
            MobileCenter.Start($"uwp={key}", typeof(Analytics));
        }

        public void Log(LagoVista.Core.PlatformSupport.LogLevel level, string area, string message, params KeyValuePair<string, string>[] args)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("Area", area);
            dictionary.Add("UseId", String.IsNullOrEmpty(_userId) ? "UNKNOWN" : _userId);
            dictionary.Add("Level", level.ToString());

            if (_args != null)
            {
                foreach (var arg in _args)
                {
                    dictionary.Add(arg.Key, arg.Value);
                }
            }

            foreach (var arg in args)
            {
                dictionary.Add(arg.Key, arg.Value);
            }

            Analytics.TrackEvent(message, dictionary);
        }

        public void LogException(string area, Exception ex, params KeyValuePair<string, string>[] args)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("Area", area);
            dictionary.Add("UseId", String.IsNullOrEmpty(_userId) ? "UNKNOWN" : _userId);
            dictionary.Add("Type", "exception");
            dictionary.Add("StackTrace", ex.StackTrace.Substring(0));

            if (_args != null)
            {
                foreach (var arg in _args)
                {
                    dictionary.Add(arg.Key, arg.Value);
                }
            }

            foreach (var arg in args)
            {
                dictionary.Add(arg.Key, arg.Value);
            }


            Analytics.TrackEvent(ex.Message, dictionary);
        }

        public void SetKeys(params KeyValuePair<String, String>[] args)
        {
            _args = args;
        }

        public void SetUserId(string userId)
        {
            _userId = userId;
        }

        public void TrackEvent(string message, Dictionary<string, string> args)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add("UseId", String.IsNullOrEmpty(_userId) ? "UNKNOWN" : _userId);

            foreach (var arg in args)
            {
                dictionary.Add(arg.Key, arg.Value);
            }

            if (_args != null)
            {
                foreach (var arg in _args)
                {
                    dictionary.Add(arg.Key, arg.Value);
                }
            }

            Analytics.TrackEvent(message, dictionary);
        }
    }
}
