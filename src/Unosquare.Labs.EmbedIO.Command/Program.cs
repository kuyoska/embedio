namespace Unosquare.Labs.EmbedIO.Command
{
    using CommandLine;
    using System;
    using System.Threading.Tasks;
    using System.Reflection;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Modules;
    using System.Net.WebSockets;
    using System.Text;
    using global::WebSocketSharp.Server;
    using global::WebSocketSharp;

    [WebSocketHandler("/echo")]
    public class EchoServer : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            var name = Context.QueryString["name"];
            Send(!string.IsNullOrEmpty(name) ? string.Format("\"{0}\" to {1}", e.Data, name) : e.Data);
        }
    }

    /// <summary>
    /// Entry poing
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Load WebServer instance
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            var options = new Options();

            Console.WriteLine("Unosquare.Labs.EmbedIO Web Server");

            var socketServer = new WebSocketServer(8080, new Log.SimpleConsoleLog());
            socketServer.AddWebSocketService<EchoServer>("/echo");

            var cts = new CancellationTokenSource();

            socketServer.RunAsync(cts.Token);

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.C)
                {
                    var webSocket = new ClientWebSocket();
                    webSocket.ConnectAsync(new Uri("ws://localhost:8080/echo"), cts.Token).Wait(cts.Token);
                    webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("HOLA")), WebSocketMessageType.Text, true, cts.Token).Wait(cts.Token);
                    var responseBytes = new ArraySegment<byte>(new byte[100]);
                    webSocket.ReceiveAsync(responseBytes, cts.Token).Wait(cts.Token);
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token).Wait(cts.Token);

                    Console.WriteLine($"RX {Encoding.UTF8.GetString(responseBytes.Array)}");
                    break;
                }
                else
                {
                    cts.Cancel();
                    Environment.Exit(1);
                }
            }

            Console.ReadLine();

            //if (!Parser.Default.ParseArguments(args, options)) return;

            //Console.WriteLine("  Command-Line Utility: Press any key to stop the server.");

            //var serverUrl = "http://localhost:" + options.Port + "/";
            //using (
            //    var server = options.NoVerbose
            //        ? WebServer.Create(serverUrl)
            //        : WebServer.CreateWithConsole(serverUrl))
            //{
            //    if (Properties.Settings.Default.UseLocalSessionModule)
            //        server.WithLocalSession();

            //    server.EnableCors().WithStaticFolderAt(options.RootPath,
            //        defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

            //    server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
            //    server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

            //    if (options.ApiAssemblies != null && options.ApiAssemblies.Count > 0)
            //    {
            //        foreach (var api in options.ApiAssemblies)
            //        {
            //            server.Log.DebugFormat("Registering Assembly {0}", api);
            //            LoadApi(api, server);
            //        }
            //    }

            //    // start the server
            //    server.RunAsync();
            //    Console.ReadKey(true);
            //}
        }

        /// <summary>
        /// Load an Assembly
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="server"></param>
        private static void LoadApi(string apiPath, WebServer server)
        {
            try
            {
                var assembly = Assembly.LoadFile(apiPath);

                if (assembly == null) return;

                server.LoadApiControllers(assembly).LoadWebSockets(assembly);
            }
            catch (Exception ex)
            {
                server.Log.Error(ex.Message);
                server.Log.Error(ex.StackTrace);
            }
        }
    }
}