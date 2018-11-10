using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                await JabberClient.Instance.JoinRoom("incursion_bot_testing@conference.goonfleet.com");
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

            // Wait for signal from other thread
            threadBlocker.WaitOne();

            // Block
            JabberClient.Instance.Disconnect().GetAwaiter().GetResult();
        }
    }
}
