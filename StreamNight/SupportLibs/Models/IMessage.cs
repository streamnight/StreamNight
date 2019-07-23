using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Models
{
    public interface IMessage
    {
        ulong Id { get; }
        long Timestamp { get; }

        ulong SenderId { get; }
        string SenderUsername { get; }
        string SenderDisplayName { get; }

        ulong GuildId { get; }
        string GuildName { get; }
        ulong ChannelId { get; }
        string ChannelName { get; }

        string OriginalText { get; }
        string OriginalHumanText { get; }
        string Attachments { get; }

        bool IsEdited { get; }
        string EditedText { get; }
        string EditedHumanText { get; }
        long EditTimestamp { get; }
    }
}
