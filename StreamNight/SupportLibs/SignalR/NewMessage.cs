using StreamNight.SupportLibs;
using StreamNight.SupportLibs.Models;
using StreamNight.SupportLibs.SignalR.Client;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.SignalR
{
    public class NewMessage : ISignalRMessage
    {
        public NewMessage(DiscordMessage message)
        {
            HumanMessage humanMessage = new HumanMessage(message);

            MessageId = message.Id.ToString();

            Author = humanMessage.SenderDisplayName;
            AuthorId = humanMessage.OriginalMessage.Author.Id.ToString();
            AuthorAvatar = humanMessage.SenderAvatarUrl.Replace(".gif", ".png");
            Content = humanMessage.HumanText;
            Timestamp = message.Timestamp.ToUnixTimeSeconds();
        }

        public NewMessage(string content)
        {
            Content = content;
        }

        [JsonConstructor]
        public NewMessage() { }

        public string Action { get { return "NewMessage"; } }
        public string MessageId { get; set; }

        public string Author { get; set; }
        public string AuthorId { get; set; }
        public string AuthorAvatar { get; set; }
        public string Content { get; set; }
        public long Timestamp { get; set; }
    }
}
