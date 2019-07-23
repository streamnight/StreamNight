using StreamNight.SupportLibs.Models;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.SignalR
{
    public class EditMessage : ISignalRMessage
    {
        public EditMessage(DiscordMessage message)
        {
            HumanMessage humanMessage = new HumanMessage(message);

            MessageId = message.Id.ToString();

            Content = humanMessage.HumanText;
        }

        [JsonConstructor]
        public EditMessage() { }

        public string Action { get { return "EditMessage"; } }
        public string MessageId { get; set; }

        public string Content { get; set; }
    }
}
