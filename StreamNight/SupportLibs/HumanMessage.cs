﻿using DSharpPlus;
using DSharpPlus.Entities;
using Markdig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs
{
    public class HumanMessage
    {
        public HumanMessage() { }

        public HumanMessage(DiscordMessage message)
        {
            OriginalMessage = message;
        }

        /// <summary>
        /// Returns a HTML-formatted string with emotes set to images.
        /// </summary>
        public string HumanText
        {
            get
            {
                if (string.IsNullOrEmpty(_HumanText))
                {
                    this.Humanise();
                }
                return _HumanText;
            }
        }

        private string _HumanText { get; set; }

        public string SenderDisplayName { get; private set; }
        public string SenderAvatarUrl { get; private set; }

        public List<RemoteEmote> Emotes { get; private set; }
        public List<RemoteChannel> Channels { get; private set; }
        public List<RemoteUser> Users { get; private set; }
        public List<RemoteRole> Roles { get; private set; }

        private DiscordMessage _OriginalMessage { get; set; }
        public DiscordMessage OriginalMessage { get { return _OriginalMessage; } set
            {
                _OriginalMessage = value;
                Humanise();
            }
        }

        private MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
                                                        .UseEmojiAndSmiley()
                                                        .UseEmphasisExtras()
                                                        .UseAutoLinks()
                                                        .Build();

        private void Humanise()
        {
            DiscordMessage message = this.OriginalMessage;
            string humanised = message.Content;

            this.SenderDisplayName = (message.Author as DiscordMember).DisplayName;
            this.SenderAvatarUrl = message.Author.AvatarUrl;

            this.Emotes = new List<RemoteEmote>();
            this.Channels = new List<RemoteChannel>();
            this.Users = new List<RemoteUser>();
            this.Roles = new List<RemoteRole>();

            // Mapping mention to names
            // Mention string is key, name with symbol (@, #) is value
            Dictionary<string, string> MentionNamePairs = new Dictionary<string, string>();

            foreach (DiscordChannel channel in message.MentionedChannels)
            {
                MentionNamePairs.Add(channel.Mention, "#" + channel.Name);
                Channels.Add(new RemoteChannel
                {
                    HumanName = "#" + channel.Name,
                    DiscordChannel = channel
                });
            }

            foreach (DiscordUser user in message.MentionedUsers)
            {
                if (user is DiscordMember mentionedMember)
                {
                    MentionNamePairs.Add(user.Mention, "@" + mentionedMember.DisplayName);
                    Users.Add(new RemoteUser
                    {
                        HumanName = "@" + mentionedMember.DisplayName,
                        DiscordUser = user
                    });
                }
                else
                {
                    MentionNamePairs.Add(user.Mention, $"@invalid user ({user.Username}#{user.Discriminator})");
                    Users.Add(new RemoteUser
                    {
                        HumanName = $"@invalid user ({user.Username}#{user.Discriminator})",
                        DiscordUser = user
                    });
                }
            }

            foreach (DiscordRole role in message.MentionedRoles)
            {
                MentionNamePairs.Add(role.Mention, "@" + role.Name);
                Roles.Add(new RemoteRole
                {
                    HumanName = "@" + role.Name,
                    DiscordRole = role
                });
            }

            foreach (KeyValuePair<string, string> mentionMap in MentionNamePairs)
            {
                humanised = humanised.Replace(mentionMap.Key, mentionMap.Value);
                humanised = humanised.Replace(mentionMap.Key.Replace("!", ""), mentionMap.Value);
            }

            // Sanitise HTML before adding emotes.
            // Emoji are passed as normal Unicode characters.
            humanised = SanitiseHtml(humanised);

            // Matches emote names and IDs, names (without colons) are group 1 ("Name"), IDs are group 2 ("Id")
            // Original Regex: @"<a?:(?<Name>[a-zA-Z0-9_~]+):(?<Id>\d+)>"
            MatchCollection emotes = Regex.Matches(humanised, @"&lt;a?:(?<Name>[a-zA-Z0-9_~]+):(?<Id>\d+)&gt;");
            foreach (Match emote in emotes)
            {
                RemoteEmote remoteEmote = new RemoteEmote
                {
                    HumanName = emote.Groups["Name"].Value,
                    Id = ulong.Parse(emote.Groups["Id"].Value)
                };
                Emotes.Add(remoteEmote);

                humanised = humanised.Replace(emote.Value, $"<img class=\"emote\" src=\"{remoteEmote.Url}\" alt=\":{remoteEmote.HumanName}:\" />");
            }

            if (message.Attachments.Count != 0)
            {
                foreach (DiscordAttachment attachment in message.Attachments)
                {
                    if (attachment.Height != 0 && !attachment.FileName.EndsWith("mp4") && !attachment.FileName.EndsWith("webm"))
                    {
                        humanised = humanised.Insert(0, $"<img class=\"discordAttachment\" src=\"{attachment.Url}\" alt=\"Message attachment\" height=\"{attachment.Height}\" width=\"{attachment.Width}\"> <br>");
                    }
                    else
                    {
                        humanised = $"(attachment posted) <br> {humanised}";
                    }
                }
            }

            if (message.Embeds.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(humanised))
                {
                    humanised = $"{humanised} <br> ";
                }
                humanised = humanised + ConvertEmbeds(message);
            }

            // Parse Markdown text
            humanised = Markdown.ToHtml(humanised, MarkdownPipeline);

            this._HumanText = humanised;
        }

        private string SanitiseHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            string output = input.Replace("&", "&amp;");
            output = output.Replace("<", "&lt;");
            output = output.Replace(">", "&gt;");
            output = output.Replace("\"", "&quot;");
            output = output.Replace("'", "&#39;");

            return output;
        }

        private string ConvertEmbeds(DiscordMessage inputMessage)
        {
            // TODO: Maybe consider looking into HtmlTags instead of manually writing HTML?
            List<string> embedHTML = new List<string>();

            foreach (DiscordEmbed embed in inputMessage.Embeds)
            {
                if (embed.Color.HasValue)
                {
                    // Embed root with colour
                    embedHTML.Add($"<div class=\"embed\" style=\"border-left-color:{embed.Color.Value.ToString()}\">");
                }
                else
                {
                    // Embed root without colour
                    embedHTML.Add($"<div class=\"embed\">");
                }

                if (embed.Author != null)
                {
                    // Embed author element
                    embedHTML.Add("<div class=\"embed-author\">");
                    if (embed.Author.IconUrl != null)
                    {
                        // Embed author image (self-closing)
                        embedHTML.Add($"<img alt=\"Embed author\" src=\"{embed.Author.IconUrl}\">");
                    }
                    if (embed.Author.Name != null)
                    {
                        // Embed author name (self-closing)
                        embedHTML.Add($"<span>{SanitiseHtml(embed.Author.Name)}</span>");
                    }
                    // Closes embed author element
                    embedHTML.Add("</div>");
                }

                if (!string.IsNullOrEmpty(embed.Title))
                {
                    // Embed title (self-closing)
                    embedHTML.Add($"<div class=\"embed-title\">{SanitiseHtml(embed.Title)}</div>");
                }

                if (!string.IsNullOrEmpty(embed.Description))
                {
                    // Embed description (self-closing)
                    embedHTML.Add($"<div class=\"embed-description\">\n\n{SanitiseHtml(embed.Description)}</div>");
                }

                if (embed.Fields != null)
                {
                    // Embed fields element
                    embedHTML.Add("<div class=\"embed-fields\">");
                    foreach (DiscordEmbedField embedField in embed.Fields)
                    {
                        if (embedField.Inline)
                        {
                            // Inline embed field
                            embedHTML.Add("<div class=\"embed-field embed-field-inline\">");
                        }
                        else
                        {
                            // Embed field
                            embedHTML.Add("<div class=\"embed-field\">");
                        }

                        if (!string.IsNullOrEmpty(embedField.Name))
                        {
                            // Embed field name (self-closing)
                            embedHTML.Add($"<div class=\"embed-field-name\">{SanitiseHtml(embedField.Name)}</div>");
                        }
                        if (!string.IsNullOrEmpty(embedField.Value))
                        {
                            // Embed field value (self-closing)
                            embedHTML.Add($"<div class=\"embed-field-value\">\n\n{SanitiseHtml(embedField.Value)}</div>");
                        }
                        // Closes embed field
                        embedHTML.Add("</div>");
                    }
                    // Closes embed fields
                    embedHTML.Add("</div>");
                }

                if (embed.Footer != null || embed.Timestamp.HasValue)
                {
                    // Embed footer element
                    embedHTML.Add("<div class=\"embed-footer\">");

                    if (embed.Footer != null)
                    {
                        if (embed.Footer.IconUrl != null)
                        {
                            embedHTML.Add($"<img src=\"{embed.Footer.IconUrl.ToString()}\">");
                        }

                        if (!string.IsNullOrEmpty(embed.Footer.Text))
                        {
                            embedHTML.Add($"<span>{SanitiseHtml(embed.Footer.Text)}</span>");
                        }
                    }

                    if (embed.Timestamp.HasValue)
                    {
                        System.Globalization.DateTimeFormatInfo formatInfo = new System.Globalization.DateTimeFormatInfo();
                        formatInfo.LongDatePattern = "yyyy-MM-ddTHH:mm:sszzz";
                        embedHTML.Add($"<time class=\"timestamp\" datetime=\"{embed.Timestamp.Value.ToString(formatInfo.LongDatePattern)}\"></time>");
                    }
                    // Closes embed footer
                    embedHTML.Add("</div>");
                }

                if (embed.Image != null)
                {
                    embedHTML.Add($"<img src=\"{embed.Image.Url}\">");
                }
                if (embed.Thumbnail != null)
                {
                    embedHTML.Add($"<img src=\"{embed.Thumbnail.Url}\">");
                }

                // Closes embed root
                embedHTML.Add("</div>");
            }

            return string.Join("\n", embedHTML);
        }

        public class RemoteEmote
        {
            public string Url
            {
                get
                {
                    return $"https://cdn.discordapp.com/emojis/{this.Id}";
                }
            }
            public string HumanName { get; set; }
            public ulong Id { get; set; }
        }

        public class RemoteUser
        {
            public string HumanName { get; set; }
            public DiscordUser DiscordUser { get; set; }
        }

        public class RemoteChannel
        {
            public string HumanName { get; set; }
            public DiscordChannel DiscordChannel { get; set; }
        }

        public class RemoteRole
        {
            public string HumanName { get; set; }
            public DiscordRole DiscordRole { get; set; }
        }
    }
}
