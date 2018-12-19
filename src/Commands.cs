using Matrix.Xmpp.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Matrix.Xmpp;
using System.Threading.Tasks;
using Matrix;
using ESI.NET.Models.Incursions;

namespace Jabber
{
    // Static only class for all of our commands
    public static class Commands
    {
        public static void Register()
        {
            //CommandDispatcher.Instance.RegisterCommand("!hello", HelloWorld);
            CommandDispatcher.Instance.RegisterCommand("!instructions", GetInstructions);
            CommandDispatcher.Instance.RegisterCommand("!setinstructions", SetInstructions);
            CommandDispatcher.Instance.RegisterCommand("!incursions", GetIncursions);
        }

        public static async Task HelloWorld(Command cmd)
        {
            var who = cmd.XmppMessage.From;

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

        public static async Task GetIncursions(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if (cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            List<Incursion> incursions = await EsiWrapper.GetIncursions();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine();

            foreach(var incursion in incursions)
            {
                builder.AppendLine(await incursion.GetDefaultIncursionMessage());
            }

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, builder.ToString());
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, builder.ToString());
            }
        }
    }
}
