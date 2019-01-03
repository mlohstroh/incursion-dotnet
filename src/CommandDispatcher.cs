using Matrix.Xmpp.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabber
{
    /// <summary>
    /// A command class for enqueuing into the dispatcher
    /// </summary>
    public class Command
    {
        public string Cmd { get; set; }
        public string Args { get; set; }
        public Message XmppMessage { get; set; }
    }

    /// <summary>
    /// A queued command dispatcher 
    /// </summary>
    public class CommandDispatcher
    { 
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

        private Dictionary<string, Func<Command, Task>> m_commands = new Dictionary<string, Func<Command, Task>>();
        // Note: This is a bit overkill for one function, but still good practice and it's very negligible on performance.
        private BlockingCollection<Command> m_commandQueue = new BlockingCollection<Command>(new ConcurrentQueue<Command>());

        /// <summary>
        /// Detects if the command is registered
        /// </summary>
        /// <param name="command">Name of the command</param>
        public bool IsCommandRegistered(string command)
        {
            return m_commands.ContainsKey(command);
        }

        public void RegisterCommand(string command, Func<Command, Task> func)
        {
            if(IsCommandRegistered(command))
            {
                Console.WriteLine("[Warning] Command {0} is attempting to register multiple times.", command);
                return;
            }

            m_commands.Add(command, func);
        }

        public void Enqueue(string command, Command cmd)
        {
            m_commandQueue.Add(cmd);
        }

        public async Task ProcessQueue()
        {
            while(true)
            {
                // Block until a packet is enqueued
                Command cmd = m_commandQueue.Take();

                // Sanity check
                if(cmd == null)
                {
                    continue;
                }

                Func<Command, Task> func = null;
                if(m_commands.TryGetValue(cmd.Cmd, out func))
                {
                    try
                    {
                        await func(cmd);
                    }
                    catch(Exception ex)
                    {
                        // Take this out at launch
                        //Debugger.Break();
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of strings. Each item in the list is an avaliable !command
        /// </summary>
        /// <returns>List<string></returns>
        public List<string> ListCommands()
        {
            List<string> commands = new List<string>();

            return m_commands.Keys.ToList();
        }
    }
}
