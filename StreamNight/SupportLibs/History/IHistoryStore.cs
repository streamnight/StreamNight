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
        /// <summary>
        /// Adds a new message to the store.
        /// </summary>
        /// <param name="message">The DiscordMessage object to add.</param>
        void AddMessage(DiscordMessage message);

        /// <summary>
        /// Updates a stored message with new contents.
        /// </summary>
        /// <param name="message">The updated DiscordMessage object.</param>
        void EditMessage(DiscordMessage message);

        /// <summary>
        /// Removes a specific message from the store.
        /// </summary>
        /// <param name="message">The DiscordMessage object to remove.</param>
        void RemoveMessage(DiscordMessage message);

        /// <summary>
        /// Removes all messages from the store.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Retrieves only the content of stored messages.
        /// </summary>
        /// <returns>A list of HTML formatted message contents.</returns>
        List<string> GetHistoryStrings();

        /// <summary>
        /// Retrieves a list of stored messages as NewMessage objects.
        /// </summary>
        /// <returns>The list of stored messages in NewMessage format.</returns>
        List<NewMessage> GetMessagesAsNewMessage();
    }
}
