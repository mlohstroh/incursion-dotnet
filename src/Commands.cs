using System;
using System.Collections.Generic;
using Matrix.Xmpp;
using System.Threading.Tasks;
using Matrix;
using jabber;
using System.Net;

namespace Jabber
{
    // Static only class for all of our commands
    public static class Commands
    {

        public const string PermissionDenied = "You do not have access to this command! Punk.";

        public static void Register()
        {
            // Set and get instructions
            CommandDispatcher.Instance.RegisterCommand("!instructions", GetInstructions);
            CommandDispatcher.Instance.RegisterCommand("!setinstructions", SetInstructions);
            
            // Get Incursions
            CommandDispatcher.Instance.RegisterCommand("!incursions", GetIncursions);

            // Add, Remove or List users who have elevated permission with the bot.
            CommandDispatcher.Instance.RegisterCommand("!adduser", SetUser);
            CommandDispatcher.Instance.RegisterCommand("!listusers", ListUsers);
            CommandDispatcher.Instance.RegisterCommand("!removeuser", RemoveUser);

            CommandDispatcher.Instance.RegisterCommand("!esi", EsiStatus);
            CommandDispatcher.Instance.RegisterCommand("!setscopes", SetEsiScopes);

            // Returns a list of available commands.
            CommandDispatcher.Instance.RegisterCommand("!ihelp", Help);

            CommandDispatcher.Instance.RegisterCommand("!iping", Test);
        }

        public static async Task Test(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            string target = cmd.Args.Trim().ToLower();
            if (target == "all")
                Broadcast.All(cmd);

            if (target == "fc")
                Broadcast.ToFcs(cmd);

            if (target == "leadership")
                Broadcast.ToLeadership(cmd);

            string response = string.Format("Invalid ping target. |  Syntax !iping {{target}} | Available targets: {0} {1} {2}\nNot all targets will be available.", "fc", "leadership", "");

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(who.Bare, response);
            }
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, response);
            }
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

            Users userObject = Users.Get();
            string message = PermissionDenied;
            if (userObject.CheckUser(author, false))
            {
                Instructions instructions = new Instructions()
                {
                    Text = cmd.Args,
                    SetAt = DateTime.UtcNow,
                    SetBy = author
                };

                instructions.Set();

                message = "Instructions set.";
            }

            if(cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, message);
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, message);
            }
        }

        public static async Task GetIncursions(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            Incursions incursionClass = Incursions.Get();
            await incursionClass.CheckIncursions();
            incursionClass.Set();

            if (cmd.XmppMessage.Type == MessageType.GroupChat)
            {
                // Lets get their jid
                Jid directJid = JabberClient.Instance.GetJidForResource(who?.Resource);

                if (directJid == null)
                {
                    Console.WriteLine("[Error] Can't reverse Resource to Jid. Resource: \"{0}\"", who?.Resource);
                    return;
                }

                await JabberClient.Instance.SendMessage(directJid, incursionClass.ToString());
            }
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, incursionClass.ToString());
            }
        }

        public static async Task SetUser(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if(cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            string message = PermissionDenied;

            Users userClass = Users.Get();
            

            if (userClass.CheckUser(author, true))
            {
                string[] parts = cmd.Args.Trim().Split(" ");
                bool setAdmin = false;
                message = "Set user help:\nAdmins: !adduser target_jabber_name --admin\nUsers: !adduser target_jabber_name";

                if (parts.Length > 0 && parts[0] != "")
                {
                    if (!parts[0].Contains('@'))
                    {
                        string new_user = parts[0];

                        if (parts.Length > 1 && parts[1].ToLower() == "--admin")
                        {
                            setAdmin = true;
                        }

                        message = userClass.AddUser(new_user, setAdmin);
                        userClass.Set();
                    }
                    else
                    {
                        message = "Do not include @goonfleet.com in the targets username.";
                    }
                }
            }

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, message);
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, message);
            }
        }

        public static async Task ListUsers(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if (cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            Users userClass = Users.Get();
            string message = PermissionDenied;


            if (userClass.CheckUser(author, true))
            {
                message = userClass.ListAll();
            }

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, message);
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, message);
            }
        }

        public static async Task RemoveUser(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if (cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            Users userClass = Users.Get();
            string message = PermissionDenied;

            if (userClass.CheckUser(author, true))
            {
                string[] parts = cmd.Args.Trim().Split(" ");
                message = "Remove user help:\n!removeuser target_jabber_name";

                if (parts.Length > 0 && parts[0] != "" && !parts[0].Contains('@'))
                {
                    message = userClass.RemoveUser(parts[0]);
                    userClass.Set();
                }
                else if(parts[0].Contains('@'))
                {
                    message = "Do not include @goonfleet.com in the targets username.";
                }
            }

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, message);
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, message);
            }
        }

        public static async Task EsiStatus(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            EsiScopes esi = EsiScopes.Get();


            if (cmd.XmppMessage.Type == MessageType.GroupChat)
            {
                // Lets get their jid
                Jid directJid = JabberClient.Instance.GetJidForResource(who?.Resource);

                if (directJid == null)
                {
                    Console.WriteLine("[Error] Can't reverse Resource to Jid. Resource: \"{0}\"", who?.Resource);
                    return;
                }

                await JabberClient.Instance.SendMessage(directJid, esi.Status());
            }
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, esi.Status());
            }
        }

        public static async Task SetEsiScopes(Command cmd)
        {
            var jid = cmd.XmppMessage.From;
            var author = jid.User;

            if (cmd.XmppMessage.IsGroupMessage())
            {
                author = jid.Resource;
            }

            EsiScopes esi = EsiScopes.Get();
            string message = PermissionDenied;

            if (Users.Get().CheckUser(author, true))
            {
                // Get an array of scopes.
                // Scope format esi-fleets,esi-ui
                string[] parts = cmd.Args.Trim().Split(" ")[0].Split(",");
                string scopesString = "";

                foreach(string s in parts)
                {
                    scopesString += s + ",";
                }
                esi.SetScopes(scopesString.Substring(0, scopesString.Length -1));
                esi.Set();

                message = "Squad ESI Scopes set.";
            }

            if (cmd.XmppMessage.IsGroupMessage())
            {
                await JabberClient.Instance.SendGroupMessage(jid.Bare, message);
            }
            else
            {
                await JabberClient.Instance.SendMessage(jid.Bare, message);
            }
        }

        public static async Task Help(Command cmd)
        {
            var who = cmd.XmppMessage.From;

            string cmd_string = "The following commands are available for the incursion bot:";
            List<string> commands = CommandDispatcher.Instance.ListCommands();
            foreach (string c in commands)
                cmd_string += string.Format("\n{0}",c);

            cmd_string += "\nSome commands require elevated permissions.";

            if (cmd.XmppMessage.Type == MessageType.GroupChat)
            {
                // Lets get their jid
                Jid directJid = JabberClient.Instance.GetJidForResource(who?.Resource);

                if (directJid == null)
                {
                    Console.WriteLine("[Error] Can't reverse Resource to Jid. Resource: \"{0}\"", who?.Resource);
                    return;
                }

                await JabberClient.Instance.SendMessage(directJid, cmd_string);
            }
            else
            {
                await JabberClient.Instance.SendMessage(who.Bare, cmd_string);
            }
        }
    }
}
