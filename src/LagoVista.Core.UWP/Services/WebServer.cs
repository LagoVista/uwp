using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Reflection;
using LagoVista.Core.Models;
using LagoVista.Core.ServiceCommon;
using LagoVista.Core.PlatformSupport;
using Newtonsoft.Json;
using LagoVista.Core.Networking.Models;
using LagoVista.Core.Networking.Interfaces;

namespace LagoVista.Core.UWP.Services
{
    public class WebServer : ServiceBase, IWebServer, IDisposable
    {
        private const uint BUFFER_SIZE = 16384;
        private readonly StreamSocketListener _streamSocketListener;

        public event EventHandler<HttpRequestMessage> MessageReceived;

        private List<IApiHandler> _handlerInstances = new List<IApiHandler>();

        public WebServer()
        {
            _streamSocketListener = new StreamSocketListener();
            _streamSocketListener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        public int Port { get; private set; }


        public void StartServer(int port)
        {
            Port = port;
#pragma warning disable CS4014
            _streamSocketListener.BindServiceNameAsync(Port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            _streamSocketListener.Dispose();
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            var request = new StringBuilder();
            using (var input = socket.InputStream)
            {
                var rdr = new StreamReader(input.AsStreamForRead());
                //var content = await rdr.ReadToEndAsync();
                //request.Append(content);
                var data = new byte[BUFFER_SIZE];
                var buffer = data.AsBuffer();
                var dataRead = BUFFER_SIZE;
                while (dataRead == BUFFER_SIZE)
                {
                    var readBuffer = await input.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, (int)BUFFER_SIZE));
                    dataRead = buffer.Length;
                }
            }

            if (request.Length > 0)
            {
                var message = HttpRequestMessage.Create(socket.OutputStream.AsStreamForWrite(), request.ToString());
                ShowDiagnostics = true;
                LogDetails("Message Received\r\n" + message.ToString());

                if (MessageReceived != null)
                    MessageReceived(this, message);
                else
                {
                    if (!await OnMessageArrived(message))
                    {
                        var response = message.GetResponseMessage();
                        response.ContentType = "text/html";
                        response.Content = "<html><head></head><body>Web Server Not Implemented</body></html>";
                        await response.Send();
                    }
                }
            }
        }


        protected virtual async Task<bool> OnMessageArrived(HttpRequestMessage msg)
        {
            foreach (var handler in _handlerInstances)
            {
                var methods = handler.GetType().GetMethods();
                foreach (var method in methods)
                {
                    var route = msg.Path.ToLower();
                    String methodRouteName = "/" + method.Name.ToLower();

                    List<Object> _argValues = new List<Object>();
                    _argValues.Add(msg);


                    var methodAttr = method.GetCustomAttribute(typeof(MethodHandlerAttribute)) as MethodHandlerAttribute;
                    if (methodAttr != null)
                    {
                        if (msg.Method.ToUpper() == methodAttr.MethodType.ToString().ToUpper())
                        {
                            //If we supplied a path for the method, use that rather than the name of the method (default)
                            if (!String.IsNullOrEmpty(methodAttr.FullPath))
                            {
                                var argIndex = 1; /* Offset by one, first parameter is the http message */
                                var routePartIndex = 0;
                                var pathParts = methodAttr.FullPath.TrimStart('/').Split('/');
                                var routeParts = route.TrimStart('/').Split('/');
                                var methodParameters = method.GetParameters();

                                route = String.Empty;
                                methodRouteName = String.Empty;

                                foreach (var part in pathParts)
                                {
                                    if (routePartIndex < routeParts.Length)
                                    {
                                        if (argIndex < methodParameters.Length)
                                        {
                                            if (part.StartsWith("{"))
                                            {
                                                var param = methodParameters[argIndex++];

                                                try
                                                {
                                                    switch (param.ParameterType.Name)
                                                    {
                                                        case "String": _argValues.Add(routeParts[routePartIndex++].ToString()); break;
                                                        case "Int32": _argValues.Add(Convert.ToInt32(routeParts[routePartIndex++])); break;
                                                        case "Guid": _argValues.Add(new Guid(routeParts[routePartIndex++])); break;
                                                        default: _argValues.Add(routeParts[routePartIndex++].ToString()); break;
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    route = String.Empty;
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                methodRouteName += String.Format("/{0}", part.ToLower());
                                                route += String.Format("/{0}", routeParts[routePartIndex++].ToLower());
                                            }
                                        }
                                        else
                                        {
                                            methodRouteName += String.Format("/{0}", part.ToLower());
                                            route += String.Format("/{0}", routeParts[routePartIndex++].ToLower());
                                        }
                                    }
                                }

                                /*If we are doing a message with a body, and the values from the query path are one less than the 
                                  method parameter list, assume the last parameter is object and attempt to deserialize. */
                                if (methodAttr.MethodType == MethodHandlerAttribute.MethodTypes.POST &&
                                    _argValues.Count == methodParameters.Count() - 1)
                                {
                                    var contentArgType = methodParameters.Last();
                                    _argValues.Add(JsonConvert.DeserializeObject(msg.Body, contentArgType.ParameterType));
                                }

                                if (_argValues.Count != methodParameters.Count())
                                    continue;
                            }

                            if (!String.IsNullOrEmpty(route) && methodRouteName == route)
                            {
                                try
                                {
                                    var responseMessage = (HttpResponseMessage)method.Invoke(handler, _argValues.ToArray());
                                    if (responseMessage != null)
                                    {
                                        await responseMessage.Send();
                                        return true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex.InnerException != null)
                                        ex = ex.InnerException;

                                    var errorMessage = msg.GetErrorMessage(ex);
                                    await errorMessage.Send();

                                }
                            }
                        }
                    }
                }
            }
            /* not happy about not understanding the code above...we need to await on the send method */
            await Task.Delay(1);
            return false;
        }

        public void RegisterAPIHandler(IApiHandler handler)
        {
            _handlerInstances.Add(handler);
        }
    }
}