using Matrix.Xmpp.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jabber
{
    // Static only class for all of our commands
    public static class Commands
    {
        public static void Register()
        {
            CommandDispatcher.Instance.RegisterCommand("!hello", HelloWorld);
        }

        public static async Task HelloWorld(Message msg)
        {
            var who = msg.From;

            await JabberClient.Instance.SendMessage(who.Bare, "Hello cruel world!");
        }
    }
}
