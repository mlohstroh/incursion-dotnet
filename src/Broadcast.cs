using System;
using System.Collections.Generic;
using jabber;
using Jabber;

namespace Jabber
{
    public static class Broadcast
    {
        /// <summary>
        /// Pings all people in a channel
        /// </summary>
        /// <remarks>Only works in channel: fcincursions</remarks>
        public static async void All(Command cmd)
        {
            if (cmd.XmppMessage.From.User == "fcincursions")
            {
                var res = JabberClient.Instance.GetJidsInRoom(cmd.XmppMessage.From.User);
                List<string> names = new List<string>();

                foreach (var kvp in res)
                {
                    if (!names.Contains(kvp.Value.User))
                        names.Add(kvp.Value.User);
                }

                await JabberClient.Instance.SendGroupMessage(cmd.XmppMessage.From.Bare, String.Join(" ", names));
            }
        }

        /// <summary>
        /// Pings bot admins in the requestors channel
        /// </summary>
        public static async void ToLeadership(Command cmd)
        {
            Users users = Users.Get();
            List<string> names = new List<string>();

            foreach (User user in users.GetAdmins())
            {
                names.Add(user.JabberResource);
            }

            await JabberClient.Instance.SendGroupMessage(cmd.XmppMessage.From.Bare, String.Join(" ", names));
        }

        /// <summary>
        /// Pings the FCs who are active in the fcincursions channel
        /// </summary>
        public static async void ToFcs(Command cmd)
        {
            var active_fcs = JabberClient.Instance.GetJidsInRoom("fcincursions");
            List<string> names = new List<string>();

            foreach (var kvp in active_fcs)
            {
                if (!names.Contains(kvp.Value.User))
                    names.Add(kvp.Value.User);
            }

            await JabberClient.Instance.SendGroupMessage(cmd.XmppMessage.From.Bare, String.Join(" ", names));
        }
    }
}
