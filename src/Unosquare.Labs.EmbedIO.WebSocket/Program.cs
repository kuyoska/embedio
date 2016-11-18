using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Unosquare.Labs.EmbedIO.Modules;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Unosquare.Labs.EmbedIO.WebSocket
{
    [WebSocketHandler("/echo")]
    public class EchoServer : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            var name = Context.QueryString["name"];
            Send(!string.IsNullOrEmpty(name) ? $"\"{e.Data}\" to {name}" : e.Data);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Unosquare.Labs.EmbedIO Web Server");

            var logger = new Log.SimpleConsoleLog();

            var server = new WebServer("http://localhost:8081", logger);
            server.WithStaticFolderAt("wwwroot");

            var socketServer = new WebSocketServer(8080, logger);
            socketServer.AddWebSocketService<EchoServer>("/echo");

            var cts = new CancellationTokenSource();

            socketServer.RunAsync(cts.Token);
            server.RunAsync(cts.Token);

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

        }
    }
}
