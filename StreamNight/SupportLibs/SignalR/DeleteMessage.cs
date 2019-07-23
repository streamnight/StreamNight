using StreamNight.SupportLibs.Models;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.SignalR
{
    public class DeleteMessage : ISignalRMessage
    {
        public DeleteMessage(DiscordMessage message)
        {
            MessageId = message.Id.ToString();
        }

        public DeleteMessage(ulong Id)
        {
            MessageId = Id.ToString();
        }

        [JsonConstructor]
        public DeleteMessage() { }

        public string Action { get { return "DeleteMessage"; } }
        public string MessageId { get; set; }
    }
}
