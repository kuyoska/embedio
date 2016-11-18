namespace Unosquare.Labs.EmbedIO
{
    using Log;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents our tiny web server used to handle requests
    /// </summary>
    public class WebServer : WebServerBase<HttpListener>
    {
        /// <summary>
        /// Gets the Url Prefix for which the server is serving requests.
        /// </summary>
        /// <value>
        /// The URL prefix.
        /// </value>
        public HttpListenerPrefixCollection UrlPrefixes => Listener.Prefixes;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        public WebServer()
            : this("http://*/", new NullLog(), RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port)
            : this(port, new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log"></param>
        public WebServer(int port, ILog log)
            : this("http://*:" + port.ToString() + "/", log, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log"></param>
        /// <param name="routingStrategy">The routing strategy</param>
        public WebServer(int port, ILog log, RoutingStrategy routingStrategy)
            : this("http://*:" + port.ToString() + "/", log, routingStrategy)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServer(string urlPrefix)
            : this(urlPrefix, new NullLog(), RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        public WebServer(string urlPrefix, ILog log)
            : this(new[] {urlPrefix}, log, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        /// <param name="routingStrategy">The routing strategy</param>
        public WebServer(string urlPrefix, ILog log, RoutingStrategy routingStrategy)
            : this(new[] {urlPrefix}, log, routingStrategy)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        public WebServer(string[] urlPrefixes)
            : this(urlPrefixes, new NullLog(), RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="log">The Log component</param>
        /// <param name="routingStrategy">The routing strategy</param>
        /// <exception cref="System.InvalidOperationException">The HTTP Listener is not supported in this OS</exception>
        /// <exception cref="System.ArgumentException">Argument urlPrefix must be specified</exception>
        public WebServer(string[] urlPrefixes, ILog log, RoutingStrategy routingStrategy)
        {
            if (HttpListener.IsSupported == false)
                throw new InvalidOperationException("The HTTP Listener is not supported in this OS");

            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            if (log == null)
                throw new ArgumentException("Argument log must be specified");

            RoutingStrategy = routingStrategy;
            Listener = new HttpListener();
            Log = log;

            foreach (var prefix in urlPrefixes)
            {
                var urlPrefix = new string(prefix?.ToCharArray());

                if (urlPrefix.EndsWith("/") == false) urlPrefix = urlPrefix + "/";
                urlPrefix = urlPrefix.ToLowerInvariant();

                Listener.Prefixes.Add(urlPrefix);
                Log.InfoFormat("Web server prefix '{0}' added.", urlPrefix);
            }

            Log.Info("Finished Loading Web Server.");
        }

        /// <summary>
        /// Handles the client request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="app"></param>
        private async void HandleClientRequest(HttpListenerContext context, Middleware app)
        {
            // start with an empty request ID
            var requestId = "(not set)";

            try
            {
                // Generate a MiddlewareContext and expected the result
                if (app != null)
                {
                    var middlewareContext = new MiddlewareContext(context, this);
                    await app.Invoke(middlewareContext);

                    if (middlewareContext.Handled) return;
                }

                // Create a request endpoint string
                var requestEndpoint = string.Join(":",
                    context.Request.RemoteEndPoint.Address.ToString(),
                    context.Request.RemoteEndPoint.Port.ToString(CultureInfo.InvariantCulture));

                // Generate a random request ID. It's currently not important butit could be useful in the future.
                requestId = string.Concat(DateTime.Now.Ticks.ToString(), requestEndpoint).GetHashCode().ToString("x2");

                // Log the request and its ID
                Log.DebugFormat("Start of Request {0}", requestId);
                Log.DebugFormat("Source {0} - {1}: {2}",
                    requestEndpoint,
                    context.RequestVerb().ToString().ToUpperInvariant(),
                    context.RequestPath());

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (ProcessRequest(context) == false)
                {
                    Log.Error("No module generated a response. Sending 404 - Not Found");
                    var responseBytes = System.Text.Encoding.UTF8.GetBytes(Constants.Response404Html);
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error handling request.", ex);
            }
            finally
            {
                // Always close the response stream no matter what.
                context?.Response.OutputStream.Close();

                Log.DebugFormat("End of Request {0}\r\n", requestId);
            }
        }

        /// <summary>
        /// Process HttpListener Request and returns true if it was handled
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        private bool ProcessRequest(HttpListenerContext context)
        {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var module in Modules)
            {
                // Establish the handler
                var handler = module.Handlers.FirstOrDefault(x =>
                    x.Path == (x.Path == ModuleMap.AnyPath ? ModuleMap.AnyPath : context.RequestPath()) &&
                    x.Verb == (x.Verb == HttpVerbs.Any ? HttpVerbs.Any : context.RequestVerb()));

                if (handler?.ResponseHandler == null)
                    continue;

                // Establish the callback
                var callback = handler.ResponseHandler;

                try
                {
                    // Inject the Server property of the module via reflection if not already there. (mini IoC ;))
                    //if (module.Server == null)
                    //    module.Server = this;

                    // Log the module and hanlder to be called and invoke as a callback.
#if NET452
                    Log.DebugFormat("{0}::{1}.{2}", module.Name, callback.Method.DeclaringType.Name,
                        callback.Method.Name);
#endif

                    // Execute the callback
                    var handleResult = callback.Invoke(this, context);
                    Log.DebugFormat("Result: {0}", handleResult.ToString());

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (context.Response.StatusCode != (int) HttpStatusCode.Unauthorized)
                    {
                        // Log the exception message.
                        var errorMessage = ex.ExceptionMessage("Failing module name: " + module.Name);
                        Log.Error(errorMessage, ex);

                        // Generate an HTML response
                        var response = string.Format(Constants.Response500HtmlFormat,
                            WebUtility.HtmlEncode(errorMessage),
                            WebUtility.HtmlEncode(ex.StackTrace));

                        // Send the response over with the corresponding status code.
                        var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    // Finally set the handled flag to true and exit.
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts the listener and the registered modules
        /// </summary>
        /// <returns>
        /// Returns the task that the HTTP listener is 
        /// running inside of, so that it can be waited upon after it's been canceled.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The method was already called.</exception>
        public override Task RunAsync(CancellationToken ct = default(CancellationToken), Middleware app = null)
        {
            if (_listenerTask != null)
                throw new InvalidOperationException("The method was already called.");

            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            Log.Info("Started HTTP Listener");
            _listenerTask = Task.Factory.StartNew(() =>
            {
                while (Listener != null && Listener.IsListening)
                {
                    try
                    {
                        var clientSocketTask = Listener.GetContextAsync();
                        clientSocketTask.Wait(ct);
                        var clientSocket = clientSocketTask.Result;

                        Task.Factory.StartNew(context => HandleClientRequest(context as HttpListenerContext, app),
                            clientSocket, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }, ct);

            return _listenerTask;
        }

        /// <summary>
        /// Starts the listener and the registered modules
        /// </summary>
        [Obsolete("Use the RunAsync method instead.", true)]
        public void Run()
        {
            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            Log.Info("Started HTTP Listener");

            ThreadPool.QueueUserWorkItem(o =>
            {
                while (Listener != null && Listener.IsListening)
                {
                    try
                    {
                        // Asynchronously queue a response by using a thread from the thread pool
                        ThreadPool.QueueUserWorkItem(contextState =>
                        {
                            // get a reference to the HTTP Listener Context
                            var context = contextState as HttpListenerContext;
                            HandleClientRequest(context, null);
                        }, Listener.GetContext());
                        // Retrieve and pass the listener context to the threadpool thread.
                    }
                    catch
                    {
                        // swallow IO exceptions
                    }
                }
            }, Listener); // Retrieve and pass the HTTP Listener
        }

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix, ILog log = null)
        {
            return new WebServer(urlPrefix, log ?? new NullLog());
        }

        #region Fluent Methods

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(int port, ILog log = null)
        {
            return new WebServer(port, log ?? new NullLog());
        }

        /// <summary>
        /// Static method to create a webserver instance using a simple console output.
        /// This method is useful for fluent configuration.
        /// </summary>
        /// <param name="urlPrefix"></param>
        /// <returns>The webserver instance.</returns>
        public static WebServer CreateWithConsole(string urlPrefix)
        {
            return new WebServer(urlPrefix, new SimpleConsoleLog());
        }

        /// <summary>
        /// Static method to create a webserver instance using a simple console output.
        /// This method is useful for fluent configuration.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        public static WebServer CreateWithConsole(string urlPrefix, RoutingStrategy routingStrategy)
        {
            return new WebServer(urlPrefix, new SimpleConsoleLog(), routingStrategy);
        }

        #endregion
    }
}