using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.SignalR
{
    public class TypingMessage
    {
        public TypingMessage(TypingStartEventArgs typingEvent)
        {
            UserId = typingEvent.User.Id.ToString();
            Timestamp = typingEvent.StartedAt.ToUnixTimeMilliseconds().ToString();
        }

        [JsonConstructor]
        public TypingMessage() { }

        public string UserId { get; set; }
        /// <summary>
        /// Note: Returns Unix time in milliseconds.
        /// </summary>
        public string Timestamp { get; set; }
    }
}
