using System;
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

                    var command = msg.Body.Trim();

                    CommandDispatcher.Instance.Enqueue(command, msg);

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

            Console.WriteLine(p.ToString());

            await m_client.SendAsync(p);

            Console.WriteLine("[Info] Joined room {0}", roomJid);
        }

        public async Task SendMessage(string jid, string message)
        {
            Jid to = new Jid(jid);

            Message msg = new Message(to, message);

            await m_client.SendAsync<Message>(msg);
        }
        
        public async Task SendGroupMessage(string jid, string message)
        {
            Jid to = new Jid(jid);

            Message msg = new Message(to, MessageType.GroupChat, message);

            await m_client.SendAsync<Message>(msg);
        }
    }
}