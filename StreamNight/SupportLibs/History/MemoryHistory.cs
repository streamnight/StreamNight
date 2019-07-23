using StreamNight.SupportLibs.SignalR;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.History
{
    public class MemoryHistory : IHistoryStore
    {
        private List<DiscordMessage> MessageList = new List<DiscordMessage>();

        public void AddMessage(DiscordMessage message)
        {
            while (MessageList.Count > 4)
            {
                MessageList.RemoveAt(0);
            }

            MessageList.Add(message);
            return;
        }

        public void EditMessage(DiscordMessage message)
        {
            int index = MessageList.FindIndex(m => m.Id == message.Id);
            if (index >= 0)
            {
                MessageList[index] = message;
            }
        }

        public void RemoveMessage(DiscordMessage message)
        {
            if (MessageList.Contains(message))
            {
                MessageList.RemoveAll(i => message.Equals(i));
            }
            return;
        }

        public void ClearHistory()
        {
            MessageList.Clear();
            return;
        }

        public List<string> GetHistoryStrings()
        {
            List<string> messageContents = new List<string>();

            foreach (DiscordMessage message in MessageList)
            {
                if (DateTime.Now.Subtract(message.Timestamp.DateTime).TotalMinutes < 5)
                {
                    messageContents.Add(new HumanMessage(message).HumanText);
                }
            }

            return messageContents;
        }

        public List<NewMessage> GetMessagesAsNewMessage()
        {
            List<NewMessage> newMessages = new List<NewMessage>();

            foreach (DiscordMessage message in MessageList)
            {
                if (DateTime.Now.Subtract(message.Timestamp.DateTime).TotalMinutes < 5)
                {
                    newMessages.Add(new NewMessage(message));
                }
            }

            return newMessages;
        }

        public List<DiscordMessage> GetRawList()
        {
            return MessageList;
        }
    }
}
