using System;
using Matrix.Xmpp.Client;

namespace Jabber
{
    public static class Extensions
    {
        public static bool IsGroupMessage(this Message message)
        {
            return message.Type == Matrix.Xmpp.MessageType.GroupChat;
        }
    }
}
