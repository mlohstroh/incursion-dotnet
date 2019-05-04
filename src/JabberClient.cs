using System;
using System.Linq;
using System.Collections.Generic;
using Matrix;
using Matrix.Srv;
using Matrix.Xmpp;
using Matrix.Xmpp.Client;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Matrix.Extensions.Client.Presence;
using Matrix.Xmpp.Muc;

namespace Jabber
{
    public class JabberClient
    {
        private static object s_lockObject = new object();
        private static JabberClient s_jabberClient = null;
        public static JabberClient Instance
        {
            get
            {
                lock(s_lockObject)
                {
                    if(s_jabberClient == null)
                    {
                        s_jabberClient = new JabberClient();
                    }

                    return s_jabberClient;
                }
            }
        }

        private XmppClient m_client = null;

        public delegate void VoidDelegate();
        public event VoidDelegate OnJabberConnected;

        private Dictionary<string, JabberRoom> m_rooms = new Dictionary<string, JabberRoom>(StringComparer.OrdinalIgnoreCase);

        public JabberClient()
        {
            string username;
            string password;
            string xmppDomain;

            // I like bitwise operations
            bool allFound = Config.GetString("JABBER_USERNAME", out username);
            allFound |= Config.GetString("JABBER_PASSWORD", out password);
            allFound |= Config.GetString("JABBER_DOMAIN", out xmppDomain);

            if (!allFound)
            {
                throw new Exception("Environment doesn't have all the necessary config variables");
            }

            m_client = new XmppClient()
            {
                Username = username,
                Password = password,
                XmppDomain = xmppDomain,
                Resource = "Bot",
                HostnameResolver = new SrvNameResolver(),
            };
        }

        public async Task Run()
        {
            if(m_client == null)
            {
                Console.WriteLine("[Error] No jabber client! Check the constructor");
                return;
            }

            m_client.XmppSessionStateObserver.Subscribe(v =>
            {
                // TODO: Alert if disabled...
                Console.WriteLine($"State changed: {v}");
            });

            m_client
                .XmppXElementStreamObserver
                .Where(el => el is Presence)
                .Subscribe(el =>
                {
                    // Unsure if we need to do something here
                    Console.WriteLine(el);
                    TrackPresenceForRooms(el as Presence);
                });

            m_client
                .XmppXElementStreamObserver
                .Where(el => el is Message)
                .Subscribe(el =>
                {
                    Message msg = el as Message;

                    // WTF XMPP?
                    if (string.IsNullOrEmpty(msg.Body))
                        return;

                    var trimmed = msg.Body.Trim();

                    var split = trimmed.Split(" ");
                    var command = split[0];
                    var args = "";

                    // Do we have any arguments?
                    if(split.Length > 1)
                    {
                        var withoutCmd = split.Skip(1).ToArray();
                        args = string.Join(" ", withoutCmd);
                    }

                    Command cmd = new Command()
                    {
                        Cmd = command,
                        Args = args,
                        XmppMessage = msg
                    };

                    CommandDispatcher.Instance.Enqueue(command, cmd);

                    Console.WriteLine("[Info] Message: {0}", el.ToString());
                });

            m_client
                .XmppXElementStreamObserver
                .Where(el => el is Iq)
                .Subscribe(el =>
                {
                    Console.WriteLine("IQ: {0}", el.ToString());
                });

            // Connect the XMPP connection
            await m_client.ConnectAsync();

            // Send our presence to the server
            await m_client.SendPresenceAsync(Show.Chat, "I'm a bot");

            // Notify delegates
            OnJabberConnected();

            // Apparently -1 is to wait forever
            await Task.Delay(-1);
        }

        public async Task Disconnect()
        {
            await m_client.DisconnectAsync();
        }

        public async Task JoinRoom(string roomJid)
        {
            string nickname;
            bool found = Config.GetString("JABBER_NICKNAME", out nickname);

            if(!found)
            {
                nickname = "Sansha_Kuvakei";
            }

            Jid to = new Jid(roomJid)
            {
                Resource = nickname
            };

            Presence p = new Presence()
            {
                To = to
            };

            p.Nick = new Matrix.Xmpp.Nickname.Nick(nickname);

            var x = new X();
            x.History = new History();
            // Suppress history, we don't want to process old messages
            x.History.MaxStanzas = 0;
            p.Add(x);

            AddRoom(to.User);

            await m_client.SendAsync(p);


            Console.WriteLine("[Info] Joined room {0}", roomJid);
        }

        public async Task SendMessage(Jid to, string message)
        {
            Message msg = new Message(to, message);

            await m_client.SendAsync<Message>(msg);
        }

        public async Task SendMessage(string jid, string message)
        {
            Jid to = new Jid(jid);

            await SendMessage(to, message);
        }
        
        public async Task SendGroupMessage(string jid, string message)
        {
            Jid to = new Jid(jid);

            Message msg = new Message(to, MessageType.GroupChat, message);

            await m_client.SendAsync<Message>(msg);
        }

        public Jid GetJidForResource(string resource)
        {
            foreach (var kvp in m_rooms)
            {
                Jid foundJid = kvp.Value.GetJidForResource(resource);
                if(foundJid != null)
                {
                    return foundJid;
                }
            }

            return null;
        }

        public Dictionary<string, Jid> GetJidsInRoom(string room = "incursion_bot_testing")
        {
            foreach (var kvp in m_rooms)
            {
                if(kvp.Key == room)
                {
                    return kvp.Value.m_resourcesToJids;
                }
            }

            return null;
        }

        private void TrackPresenceForRooms(Presence p)
        {
            foreach(var kvp in m_rooms)
            {
                if(kvp.Key == p.From.User)
                {
                    kvp.Value.AddUser(p);
                }
            }
        }

        private void AddRoom(string jid)
        {
            JabberRoom existing;
            if(!m_rooms.TryGetValue(jid, out existing))
            {
                existing = new JabberRoom()
                {
                    Jid = jid
                };
                m_rooms[jid] = existing;
            }
        }

        internal object GetJidsInRoom(object user)
        {
            throw new NotImplementedException();
        }
    }
}