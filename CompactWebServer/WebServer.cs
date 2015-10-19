using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections.Specialized;
using System.Reflection;

namespace CompactWebServer
{
    /// <summary>
    /// Class implements functionality of the simple web server
    /// </summary>
    public class WebServer
    {
        #region Fields
        
        TcpListener tcpListener;
        Thread mainThread;
        bool serverStop = false;
        bool running = false;

        public WebServerConfiguration Configuration;
        #endregion

        /// <summary>
        /// New WebServer
        /// </summary>
        /// <param name="webServerConf">WebServer Configuration</param>
        public WebServer(WebServerConfiguration webServerConf)
        {
            this.Configuration = webServerConf;
            Type[] allTypes = Assembly.GetCallingAssembly().GetTypes();
            RegisterRoutes(allTypes);
        }

        /// <summary>
        /// Starts the WebServer thread
        /// </summary>
        public void Start()
        {
            try
            {
                tcpListener = new TcpListener(Configuration.IPAddress, Configuration.Port);
                tcpListener.Start();
                mainThread = new Thread(new ThreadStart(StartListen));
                serverStop = false;
                mainThread.Start();
                running = true;
                RaiseLogEvent("debug", "server started");
            }
            catch (Exception e)
            {
                RaiseLogEvent("error", e.ToString());
            }
        }

        /// <summary>
        /// Stops the WebServer thread
        /// </summary>
        public void Stop()
        {
            try
            {
                if (mainThread != null)
                {
                    serverStop = true;
                    tcpListener.Stop();
                    mainThread.Join(1000);
                    running = false;
                    RaiseLogEvent("debug", "server stopped");
                }
            }
            catch (Exception e)
            {
                RaiseLogEvent("error", e.ToString());
            }
        }

        private void RegisterRoutes(Type[] allTypes)
        {
            string httpReqFullName = typeof(HttpRequest).FullName;
            string httpResFullName = typeof(HttpResponse).FullName;

            // Type[] allTypes = Assembly.GetCallingAssembly().GetTypes();
            Type baseControllerType = typeof(BaseController);
            foreach(Type controller in allTypes)
            {
                // check if controller is child of BaseController
                if(controller.IsSubclassOf(baseControllerType))
                {
                    // get all methods
                    MethodInfo[] methods = controller.GetMethods();

                    // loop in all methods and process each one
                    foreach (MethodInfo method in methods)
                    {
                        ParameterInfo[] parameters = method.GetParameters();

                        // only process public method has 2 parameters with one is HttpRequest and another is HttpResponse
                        if (!method.IsPublic || !(parameters.Length == 2)
                            || !parameters[0].ParameterType.FullName.Equals(httpReqFullName)
                            || !parameters[1].ParameterType.FullName.Equals(httpResFullName))
                            continue;

                        // check route attributes  if exists
                        object[] attributes = method.GetCustomAttributes(typeof(RouteAttribute), false);
                        if (attributes.Length > 0)
                        {
                            // if route attributes  exists, add route for it
                            string routeAttributeFullName = typeof(RouteAttribute).FullName;
                            foreach (RouteAttribute attribute in attributes)
                            {
                                // add to router
                                this.Configuration.Router.Add(attribute.Method, attribute.Path, method);
                            }
                        }
                        else
                        {
                            // if attribute route does not exist, build route base on class name and method name

                            // replace the convention Controller
                            string controllerName = controller.Name.Replace("Controller", "").ToLower();
                            string actionName = method.Name.ToLower();
                            // build the path
                            string path = string.Format("/{0}/{1}", controllerName, actionName);
                            // add to router
                            this.Configuration.Router.Add(path, method);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start listening on port
        /// </summary>
        private void StartListen()
        {
            while (!serverStop)
            {
                try {
                    var tcpClient = tcpListener.AcceptTcpClient();
                    if (tcpClient != null && tcpClient.Client.Connected)
                    {
#if DEBUG
                        HandleRequest(tcpClient);
#else
                        ThreadPool.QueueUserWorkItem(HandleRequest, tcpClient);
#endif
                    }
                }
                catch (Exception ex)
                {
                    RaiseLogEvent("debug", string.Format("Server error: {0}", ex.Message));
                }

                Thread.Sleep(100);
            }
        }

        private void HandleRequest(object _tcpClient)
        {
            TcpClient tcpClient = (TcpClient)_tcpClient;

            try
            {
                RaiseLogEvent("debug", string.Format("client {0} connected", tcpClient.Client.RemoteEndPoint.ToString()));

                HttpRequest request = null;

                try { request = new HttpRequest(this, tcpClient); }
                catch (Exception ex1) { RaiseLogEvent("error", ex1.ToString()); }

                HttpResponse response = new HttpResponse(this, tcpClient);

                if (request == null)
                    response.SendError(StatusCode.BadRequest, "Bad request");
                else if (!this.Configuration.Router.Contain(request.Method, request.Path))
                    response.SendError(StatusCode.NotFound, "Page not found");
                else
                {
                    MethodInfo webCtrl = this.Configuration.Router.Get(request.Method, request.Path);

                    ConstructorInfo ctor = webCtrl.DeclaringType.GetConstructor(new Type[]{});

                    object obj = ctor.Invoke(new Type[] { });

                    try { webCtrl.Invoke(obj, new object[] { request, response }); }
                    catch { response.SendError(StatusCode.InternalServerError, "Internal server error"); }
                }
            }
            catch (Exception ex)
            {
                RaiseLogEvent("error", ex.ToString());
                if (tcpClient != null)
                {
                    tcpClient.Client.Close();
                    tcpClient.Close();
                }
            }
        }

        /// <summary>
        /// Raise event when something "loggable" happend
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="message">Message</param>
        public void RaiseLogEvent(string eventType, string message)
        {
            if (OnLogEvent != null)
            {
                OnLogEvent(eventType, message);
            }
        }

        /// <summary>
        /// Determines whether WebServer is running or not
        /// </summary>
        public bool Running
        {
            get { return running; }
        }

        public delegate void LogEvent(string type, string message);
        /// <summary>
        /// Event is Raised when log event occures
        /// </summary>
        public event LogEvent OnLogEvent;   

    }
}
