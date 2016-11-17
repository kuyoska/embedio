#if NET452
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Log;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace Unosquare.Labs.EmbedIO
{
    /// <summary>
    /// Represents our tiny web server used to handle web socket requests
    /// </summary>
    public class WebSocketServer : WebServerBase<TcpListener>
    {
        public WebSocketServer()
            : this(8080, new NullLog())
        {
            // placeholder
        }

        public WebSocketServer(int port)
            : this(port, new NullLog())
        {
            // placeholder
        }

        public WebSocketServer(int port, ILog log)
        {
            if (log == null)
                throw new ArgumentException("Argument log must be specified");

            Listener = new TcpListener(System.Net.IPAddress.Any, port);
            Log = log;

            Log.InfoFormat("Web socket server port '{0}' added.", port);
            // TODO: Implement address location

            Log.Info("Finished Loading Web Socket Server.");
        }

        private bool checkServicePath(string path, out string message)
        {
            message = null;

            if (string.IsNullOrEmpty(path))
            {
                message = "'path' is null or empty.";
                return false;
            }

            if (path[0] != '/')
            {
                message = "'path' is not an absolute path.";
                return false;
            }

            if (path.IndexOfAny(new[] { '?', '#' }) > -1)
            {
                message = "'path' includes either or both query and fragment components.";
                return false;
            }

            return true;
        }

        private Dictionary<string, WebSocketBehavior> _services = new Dictionary<string, WebSocketBehavior>();

        public void AddWebSocketService<T>(string path)
            where T : WebSocketBehavior
        {
            string msg;
            if (!checkServicePath(path, out msg))
            {
                Log.Error(msg);
                return;
            }

            _services.Add(path, Activator.CreateInstance<T>());
        }

        public override Task RunAsync(CancellationToken ct = default(CancellationToken), Middleware app = null)
        {
            if (_listenerTask != null)
                throw new InvalidOperationException("The method was already called.");

            /*
             * if (_reuseAddress)
        _listener.Server.SetSocketOption (
          SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);*/

            Listener.Start();

            Log.Info("Started Tcp Listener");
            _listenerTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        var cl = Listener.AcceptTcpClient();
                        var ctx = new TcpListenerWebSocketContext(cl, null, false, Log);
                        Task.Factory.StartNew(context => HandleClientRequest(context as TcpListenerWebSocketContext, app), ctx, ct);
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

        private void HandleClientRequest(TcpListenerWebSocketContext context, Middleware app)
        {
            var uri = context.RequestUri;
            if (uri == null)
            {
                context.Close(HttpStatusCode.BadRequest);
                return;
            }
            
            var finalPath = HttpUtility.UrlDecode(uri.AbsolutePath).TrimEndSlash();
            if (_services.ContainsKey(finalPath) == false)
            {
                context.Close(HttpStatusCode.NotImplemented);
                return;
            }

            _services[finalPath].Start(context, Log);
        }
    }
}
#endif