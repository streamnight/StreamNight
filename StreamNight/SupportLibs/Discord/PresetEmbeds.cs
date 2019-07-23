using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamNight.SupportLibs.Discord
{
    public static class PresetEmbeds
    {
        public static DiscordEmbedBuilder ErrorEmbed(string errorMessage, DiscordMessage message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Chat Bridge Bot",
                Description = errorMessage,
                Author = new DiscordEmbedBuilder.EmbedAuthor(),
                Color = DiscordColor.Red,
                Timestamp = DateTimeOffset.UtcNow
            };
            embed.Author.Name = message.Author.Username + "#" + message.Author.Discriminator;
            embed.Author.IconUrl = message.Author.AvatarUrl;

            return embed;
        }

        public static DiscordEmbedBuilder SuccessEmbed(string successMessage, DiscordMessage message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Chat Bridge Bot",
                Description = successMessage,
                Author = new DiscordEmbedBuilder.EmbedAuthor(),
                Color = DiscordColor.Green,
                Timestamp = DateTimeOffset.UtcNow
            };
            embed.Author.Name = message.Author.Username + "#" + message.Author.Discriminator;
            embed.Author.IconUrl = message.Author.AvatarUrl;

            return embed;
        }

        public static DiscordEmbedBuilder InfoEmbed(string infoMessage, DiscordMessage message)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder
            {
                Title = "Chat Bridge Bot",
                Description = infoMessage,
                Author = new DiscordEmbedBuilder.EmbedAuthor(),
                Color = DiscordColor.CornflowerBlue,
                Timestamp = DateTimeOffset.UtcNow
            };
            embed.Author.Name = message.Author.Username + "#" + message.Author.Discriminator;
            embed.Author.IconUrl = message.Author.AvatarUrl;

            return embed;
        }
    }
}
