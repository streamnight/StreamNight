using StreamNight.SupportLibs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs
{
    public class Message : IMessage
    {
        public ulong Id { get; private set; }
        public long Timestamp { get; private set; }

        public ulong SenderId { get; private set; }
        public string SenderUsername { get; private set; }
        public string SenderDisplayName { get; private set; }

        public ulong GuildId { get; private set; }
        public string GuildName { get; private set; }
        public ulong ChannelId { get; private set; }
        public string ChannelName { get; private set; }

        public string OriginalText { get; private set; }
        public string OriginalHumanText { get; private set; }
        public string Attachments { get; private set; }

        public bool IsEdited { get; private set; }
        public string EditedText { get; private set; }
        public string EditedHumanText { get; private set; }
        public long EditTimestamp { get; private set; }
    }
}
