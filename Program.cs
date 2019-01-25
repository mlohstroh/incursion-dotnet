using jabber;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using uhttpsharp;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace Jabber
{
    class Program
    {
        static void Main(string[] args)
        {
            ManualResetEvent threadBlocker = new ManualResetEvent(false);

            // Wait for control+c
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                threadBlocker.Set();
            };

            JabberClient.Instance.OnJabberConnected += async delegate ()
            {
                //await JabberClient.Instance.JoinRoom("incursion_bot_testing@conference.goonfleet.com");
                await JabberClient.Instance.JoinRoom("incursion-leadership@conference.goonfleet.com");
                await JabberClient.Instance.JoinRoom("fcincursions@conference.goonfleet.com");
                await JabberClient.Instance.JoinRoom("incursions@conference.goonfleet.com");
            };

            Commands.Register();

            // TODO: I'm not sure if this is the best way to create one off things. Seems to be in a console app.
            Task jabberTask = new Task(async () =>
            {
                await JabberClient.Instance.Run();
            }, TaskCreationOptions.LongRunning);


            Task commandTask = new Task(async () =>
            {
                await CommandDispatcher.Instance.ProcessQueue();
            }, TaskCreationOptions.LongRunning);


            jabberTask.Start();
            commandTask.Start();

            DateTime now = DateTime.Now;
            Scheduler.IntervalInMinutes(now.Hour, now.Minute + 1, 5, async () =>
            {
                Incursions inc = Incursions.Get();
                await inc.UpdateIncursions();
                inc.Set();
            });


            using(var httpServer = new HttpServer(new HttpRequestProvider()))
            {
                Config.GetInt("http_port", out int port);
                httpServer.Use(new TcpListenerAdapter(new TcpListener(IPAddress.Loopback, port)));

                // Request handling : 
                httpServer.Use((context, next) => {
                    return next();
                });

                httpServer.Start();

                // Wait for signal from other thread
                threadBlocker.WaitOne();
            }

            // Block
            JabberClient.Instance.Disconnect().GetAwaiter().GetResult();
        }
    }
}
