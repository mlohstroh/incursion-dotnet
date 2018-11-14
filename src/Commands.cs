using Matrix.Xmpp.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Matrix.Xmpp;
using System.Threading.Tasks;
using ESIClient.Dotcore.Api;
using ESIClient.Dotcore.Client;
using ESIClient.Dotcore.Model;
using Matrix;

namespace Jabber
{
    // Static only class for all of our commands
    public static class Commands
    {
        public static void Register()
        {
            CommandDispatcher.Instance.RegisterCommand("!hello", HelloWorld);
            CommandDispatcher.Instance.RegisterCommand("!instructions", GetInstructions);
            CommandDispatcher.Instance.RegisterCommand("!setinstructions", SetInstructions);
        }

        public static async Task HelloWorld(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            var apiInstance = new IncursionsApi();

            try
            {
                var tmp = apiInstance.GetIncursions();
                Console.WriteLine(tmp[0]);
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception hen calling IncursionsApi.GetInstance: " + e.Message);
            }



            if (cmd.XmppMessage.Type == MessageType.GroupChat)
                await JabberClient.Instance.SendGroupMessage(who.Bare, "Hello Cruel World!");
            else
                await JabberClient.Instance.SendMessage(who.Bare, "Hello Cruel World!");
        }

        public static async Task GetInstructions(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            Instructions instructions = Instructions.Get();

            string reply = "No instructions set!";

            if(instructions != null)
            {
                reply = instructions.ToString();
            }

            if (cmd.XmppMessage.Type == MessageType.GroupChat)
            {
                // Lets get their jid
                Jid directJid = JabberClient.Instance.GetJidForResource(who?.Resource);

                if(directJid == null)
                {
                    Console.WriteLine("[Error] Can't reverse Resource to Jid. Resource: \"{0}\"", who?.Resource);
                    return;
                }

                await JabberClient.Instance.SendMessage(directJid, reply);
            } 
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, reply);
            }
        }

        public static async Task SetInstructions(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if (cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            Instructions instructions = new Instructions()
            {
                Text = cmd.Args,
                SetAt = DateTime.UtcNow,
                SetBy = author
            };

            instructions.Set();

            if(cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, "Instructions set!");
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, "Instructions set!");
            }
        }
    }
}
