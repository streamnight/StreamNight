using StreamNight.SupportLibs.SignalR;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.History
{
    public interface IHistoryStore
    {
        void AddMessage(DiscordMessage message);
        void EditMessage(DiscordMessage message);
        void RemoveMessage(DiscordMessage message);
        void ClearHistory();
        List<string> GetHistoryStrings();
        List<NewMessage> GetMessagesAsNewMessage();
    }
}
