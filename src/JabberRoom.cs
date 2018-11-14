using System;
using System.Collections.Generic;
using Matrix;
using Matrix.Xmpp.Client;

namespace Jabber
{
    /// <summary>
    /// An abstraction of what a jabber room will look like. 
    /// It will be able to reverse a group message author to a jid
    /// </summary>
    public class JabberRoom
    {
        public string Jid { get; set; }

        public Dictionary<string, Jid> m_resourcesToJids = new Dictionary<string, Jid>();

        public void AddUser(Presence presence)
        {
            if(presence.From?.User != Jid)
                return;

            string resource = presence.From?.Resource;
            string userJid = presence?.MucUser?.Item?.Jid;

            if (string.IsNullOrEmpty(resource) || string.IsNullOrEmpty(userJid))
                return; // bad presence?

            m_resourcesToJids[resource] = userJid;
        }

        public Jid GetJidForResource(string resource)
        {
            Jid jid;
            if(!m_resourcesToJids.TryGetValue(resource, out jid))
            {
                return null;
            }

            return jid;
        }
    }
}
