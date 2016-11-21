#if NET452
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Log;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace Unosquare.Labs.EmbedIO
{
    /// <summary>
    /// Represents our tiny web server used to handle web socket requests
    /// </summary>
    public class WebSocketServer : WebServerBase<TcpListener>
    {
        internal class HttpContext
        {
            internal HttpRequest Request { get; set; }

            internal TcpClient Connection { get; set; }
        }

        private readonly Dictionary<string, WebSocketBehavior> _services = new Dictionary<string, WebSocketBehavior>();

        public string FileSystemPath { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class.
        /// </summary>
        public WebSocketServer()
            : this(8080, new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="rootPath">The root path.</param>
        public WebSocketServer(int port, string rootPath)
            : this(port, new NullLog(), rootPath)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log">The log.</param>
        /// <param name="rootPath">The root path.</param>
        /// <exception cref="System.ArgumentException">Argument log must be specified</exception>
        public WebSocketServer(int port, ILog log, string rootPath = null)
        {
            if (log == null)
                throw new ArgumentException("Argument log must be specified");

            FileSystemPath = rootPath ?? typeof(WebSocketServer).Assembly.Location;

            Listener = new TcpListener(IPAddress.Any, port);
            Log = log;

            Log.InfoFormat("Web socket server port '{0}' added.", port);
            // TODO: Implement address location

            Log.Info("Finished Loading Web Socket Server.");
        }

        private bool CheckPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error($"'{nameof(path)}' is null or empty.");
                return false;
            }

            if (path[0] != '/')
            {
                Log.Error($"'{nameof(path)}' is not an absolute path.");
                return false;
            }

            if (path.IndexOfAny(new[] {'?', '#'}) <= -1) return true;

            Log.Error($"'{nameof(path)}' includes either or both query and fragment components.");
            return false;
        }
        
        /// <summary>
        /// Adds the web socket service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        public void AddWebSocketService<T>(string path)
            where T : WebSocketBehavior
        {
            if (CheckPath(path) == false) return;

            _services.Add(path, Activator.CreateInstance<T>());
        }

        /// <summary>
        /// Runs the asynchronous.
        /// </summary>
        /// <param name="ct">The ct.</param>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The method was already called.</exception>
        public override Task RunAsync(CancellationToken ct = default(CancellationToken), Middleware app = null)
        {
            if (_listenerTask != null)
                throw new InvalidOperationException("The method was already called.");

            // TODO: Resuse?
            //Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            Listener.Start();

            Log.Info("Started TCP Listener");
            _listenerTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var cl = Listener.AcceptTcpClient();
                        var stream = cl.GetStream();
                        var request = HttpRequest.Read(stream, 90000);

                        if (request.IsWebSocketRequest)
                        {
                            Log.Info("Process websocket request");

                            var ctx = new WebSocketContext(cl, request, null, false, Log);
                            Task.Factory.StartNew(context => HandleClientRequest(context as WebSocketContext), ctx, ct);
                        }
                        else
                        {
                            Log.Info("Process web request");

                            var ctx = new HttpContext() {Request = request, Connection = cl};
                            Task.Factory.StartNew(context => HandleWebRequest(context as HttpContext), ctx, ct);
                        }
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

        private void HandleWebRequest(HttpContext context)
        {
            HttpResponse response = null;

            try
            {
                var path = context.Request.RequestUri.Replace('/', Path.DirectorySeparatorChar);

                if (path.Last() == Path.DirectorySeparatorChar)
                    path = path + "index.html";
                path = Path.Combine(FileSystemPath, path);

                if (File.Exists(path))
                {
                    response = new HttpResponse(HttpStatusCode.OK);
                    response.Write(File.ReadAllBytes(path));
                }
            }
            catch (Exception ex)
            {
                // Log the exception message.
                Log.Error(ex.Message, ex);

                response = new HttpResponse(HttpStatusCode.InternalServerError);

                var message = string.Format(Constants.Response500HtmlFormat,
                    WebUtility.HtmlEncode(ex.Message),
                    WebUtility.HtmlEncode(ex.StackTrace));

                // Generate an HTML response
                response.Write(Encoding.UTF8.GetBytes(message));
            }

            // Not found
            if (response == null) response = new HttpResponse(HttpStatusCode.NotFound);
            context.Connection.Client.Send(Encoding.UTF8.GetBytes(response.ToString()));
            context.Connection.Client.Close();
        }
        
        private void HandleClientRequest(WebSocketContext context)
        {
            var uri = context.RequestUri;
            if (uri == null)
            {
                context.Close(HttpStatusCode.BadRequest);
                return;
            }
            
            var finalPath = WebUtility.UrlDecode(uri.AbsolutePath).TrimEnd('/');
            finalPath = finalPath.Length > 0 ? finalPath : "/";

            if (_services.ContainsKey(finalPath) == false)
            {
                context.Close(HttpStatusCode.NotFound);
                return;
            }

            _services[finalPath].Start(context, Log);
        }
    }
}
#endif