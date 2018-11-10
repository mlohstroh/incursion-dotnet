using Matrix.Xmpp.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jabber
{
    /// <summary>
    /// A queued command dispatcher 
    /// </summary>
    public class CommandDispatcher
    {
        private class CommandPacket
        {
            public string Command { get; set; }
            public Message XmppMessage { get; set; }
        }

        // Singleton Pattern, quick and simple
        private static object s_lockObject = new object();
        private static CommandDispatcher s_commandDispatcher = null;
        public static CommandDispatcher Instance
        {
            get
            {
                lock (s_lockObject)
                {
                    if (s_commandDispatcher == null)
                    {
                        s_commandDispatcher = new CommandDispatcher();
                    }

                    return s_commandDispatcher;
                }
            }
        }

        private Dictionary<string, Func<Message, Task>> m_commands = new Dictionary<string, Func<Message, Task>>();
        // Note: This is a bit overkill for one function, but still good practice and it's very negligible on performance.
        private BlockingCollection<CommandPacket> m_commandQueue = new BlockingCollection<CommandPacket>(new ConcurrentQueue<CommandPacket>());

        public bool IsCommandRegistered(string command)
        {
            return m_commands.ContainsKey(command);
        }

        public void RegisterCommand(string command, Func<Message, Task> func)
        {
            if(IsCommandRegistered(command))
            {
                Console.WriteLine("[Warning] Command {0} is attempting to register multiple times.", command);
                return;
            }

            m_commands.Add(command, func);
        }

        public void Enqueue(string command, Message msg)
        {
            CommandPacket packet = new CommandPacket()
            {
                Command = command,
                XmppMessage = msg
            };

            m_commandQueue.Add(packet);
        }

        public async Task ProcessQueue()
        {
            while(true)
            {
                // Block until a packet is enqueued
                CommandPacket packet = m_commandQueue.Take();

                // Sanity check
                if(packet == null)
                {
                    continue;
                }

                Func<Message, Task> func = null;
                if(m_commands.TryGetValue(packet.Command, out func))
                {
                    await func(packet.XmppMessage);
                }
            }
        }
    }
}
