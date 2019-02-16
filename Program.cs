using jabber;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jabber
{
    class Program
    {
        static void Main(string[] args)
        {
            JabberClient.Instance.OnJabberConnected += async delegate ()
            {
                await JabberClient.Instance.JoinRoom("incursion_bot_testing@conference.goonfleet.com");
                //await JabberClient.Instance.JoinRoom("incursion-leadership@conference.goonfleet.com");
                //await JabberClient.Instance.JoinRoom("fcincursions@conference.goonfleet.com");
                //await JabberClient.Instance.JoinRoom("incursions@conference.goonfleet.com");
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

            // Updates incursions every five minutes.
            Scheduler.IntervalInMinutes(now.Hour, now.Minute + 1, 5, async () =>
            {
                Incursions inc = Incursions.Get();
                await inc.UpdateIncursions();
                inc.Set();
            });

            // Checks for application updates every 60 minutes
            // If an update is pending - update the software.
            Scheduler.IntervalInMinutes(now.Hour, now.Minute + 1, 1, () =>
            {
                if (UpdateManager.UpdatePending())
                {
                    // Update things here?
                }
            });


            CreateWebHostBuilder(args).Build().Run();

            // Block
            JabberClient.Instance.Disconnect().GetAwaiter().GetResult();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            // Get listen urls by default
            string listenUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            int port = 5000;
            if(Config.GetInt("PORT", out port))
            {
                listenUrl = string.Format("http://*:{0};https://*:{1}", port, port + 1);
            }

            return WebHost.CreateDefaultBuilder(args)
                .UseUrls(listenUrl)
                .UseStartup<Startup>();
        }
    }
}
