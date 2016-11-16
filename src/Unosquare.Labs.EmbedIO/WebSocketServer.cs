#if NET452
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Log;

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
                        //var ctx = new TcpListenerWebSocketContext(

                        //var clientSocketTask = Listener.GetContextAsync();
                        //clientSocketTask.Wait(ct);
                        //var clientSocket = clientSocketTask.Result;

                        //Task.Factory.StartNew(context => HandleClientRequest(context as HttpListenerContext, app),
                        //    clientSocket, ct);
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
    }
}
#endif