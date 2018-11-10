using Matrix.Xmpp.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Matrix.Xmpp;
using System.Threading.Tasks;

namespace Jabber
{
    // Static only class for all of our commands
    public static class Commands
    {
        public static void Register()
        {
            CommandDispatcher.Instance.RegisterCommand("!hello", HelloWorld);
            CommandDispatcher.Instance.RegisterCommand("!instructions", GetInstructions);
        }

        public static async Task HelloWorld(Message msg)
        {
            var who = msg.From;

            if (msg.Type == MessageType.GroupChat)
                await JabberClient.Instance.SendGroupMessage(who.Bare, "Hello Cruel World!");
            else
                await JabberClient.Instance.SendMessage(who.Bare, "Hello Cruel World!");
        }

        public static async Task GetInstructions(Message msg)
        {
            var who = msg.From;
            Instruction tmpInstruction = new Instruction("This is our instructions", "samuel_the_terrible");

            if (msg.Type == MessageType.GroupChat)
            {
                await JabberClient.Instance.SendGroupMessage(who.Bare, tmpInstruction.ToString());
            } 
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, tmpInstruction.ToString());
            }
        }
    }
}
