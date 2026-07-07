using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Treci_projekat
{
    public class AppHost
    {
        public async Task RunAsync()
        {
            var sentimentService = new SentimentService();
            var system = ActorSystem.Create("GutenbergSystem", SystemConfig.GetAkkaConfig());

            var pollInterval = TimeSpan.FromMinutes(2);
            var stateActor = system.ActorOf(StateActor.CreateProps(pollInterval, sentimentService), "state");

            var server = new WebServer(stateActor);
            server.Start();

            var cts = new CancellationTokenSource();
            RegisterShutdownHandlers(server, cts);
            await server.RunAsync(cts.Token);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server se isključuje...");
            server.Close();
            await system.Terminate();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server je isključen");
        }

        private static void RegisterShutdownHandlers(WebServer server, CancellationTokenSource cts)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Signal za isključivanje (Ctrl+C)");
                cts.Cancel();
                server.Stop();
            };
            _ = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var line = Console.ReadLine();
                    if (line?.Equals("q", StringComparison.OrdinalIgnoreCase) == true ||
                        line?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Signal za isključivanje ('q')");
                        cts.Cancel();
                        server.Stop();
                        break;
                    }
                }
            });
        }
    }
}
