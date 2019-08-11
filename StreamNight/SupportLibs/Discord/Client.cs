﻿using StreamNight.SupportLibs.History;
using StreamNight.SupportLibs.Models;
using StreamNight.SupportLibs.SignalR;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using StreamNight.SupportLibs.Status;

namespace StreamNight.SupportLibs.Discord
{
    public class Client
    {
        private CancellationTokenSource stopClientTokenSource;

        internal BotConfig botConfig;
        private DiscordClient discordClient;
        private DiscordWebhookClient webhookClient;
        /// <summary>
        /// The message storage location that is used to send historical messages to connecting clients.
        /// </summary>
        public IHistoryStore historyStore;

        // Putting this here to mimic singleton behaviour.
        /// <summary>
        /// The SystemStatus object tracking this client. Null until the status page is accessed.
        /// </summary>
        public SystemStatus SystemStatus = null;

        internal MessageHandler messageHandler;

        /// <summary>
        /// Signals whether the stream is up.
        /// </summary>
        public bool StreamUp = false;
        /// <summary>
        /// Signals whether the Discord client is running.
        /// </summary>
        public bool Running = false;
        /// <summary>
        /// Signals whether the Discord client has successfully connected to the guild.
        /// </summary>
        public bool Ready = false;
        /// <summary>
        /// Signals whether the webhook's channel matches the channel ID set in the config.
        /// </summary>
        public bool WebhookChannelMatch = false;
        public bool UseServerLogo = false;
        private ulong GuildId = 0;
        private ulong ChannelId = 0;
        private string ClientName;

        private string ShortServerName;

        /// <summary>
        /// The string used to generate the ASP.NET page titles.
        /// </summary>
        public string StreamName;
        /// <summary>
        /// The stream channel's Discord name.
        /// </summary>
        public string StreamChannelName;

        public string StreamRole;
        public string AdminRole;

        public string LogoWebPath
        {
            get
            {
                if (this.UseServerLogo)
                {
                    if (this.Ready && (DateTime.UtcNow - LastLogoRefresh).TotalMinutes > 60.0)
                    {
                        _ = this.DownloadGuildLogo();
                        LastLogoRefresh = DateTimeOffset.UtcNow;
                    }
                    return _LogoWebPath;
                }
                else
                {
                    return "/images/staticlogo.png";
                }
            }
        }
        private string _LogoWebPath = "/images/staticlogo.png";
        private DateTimeOffset LastLogoRefresh = DateTimeOffset.UnixEpoch;

        public Client(IHistoryStore historyStore)
        {
            this.historyStore = historyStore;
        }

        /// <summary>
        /// Initialises and starts the Discord bot.
        /// </summary>
        /// <returns>The task representing the bot's execution status.</returns>
        public async Task RunBotAsync()
        {
            this.Running = true;
            stopClientTokenSource = new CancellationTokenSource();

            var json = "";
            using (var fs = File.OpenRead("botconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            this.botConfig = JsonConvert.DeserializeObject<BotConfig>(json);

            this.UseServerLogo = this.botConfig.UseServerLogo;
            this.StreamRole = this.botConfig.StreamRole;
            this.AdminRole = this.botConfig.AdminRole;
            this.StreamName = this.botConfig.StreamName;
            if (!string.IsNullOrEmpty(this.botConfig.ShortServerName))
            {
                ShortServerName = this.botConfig.ShortServerName;
            }

            var cfg = new DiscordConfiguration
            {
                Token = this.botConfig.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            messageHandler = new MessageHandler(new MessageHandlerConfig
            {
                ApiUrl = this.botConfig.ApiUrl,
                ChannelId = this.botConfig.ChannelId,
                HmacKey = this.botConfig.HmacKey,
                // Login token generator
                TokenKey = this.botConfig.TokenKey,
                TokenPassword = this.botConfig.TokenPassword,
                StreamRole = this.botConfig.StreamRole,
                // Reference to this
                Client = this
            });

            Uri.TryCreate(this.botConfig.WebhookUrl, UriKind.Absolute, out Uri WebhookUri);
            GuildId = this.botConfig.GuildId;
            ChannelId = this.botConfig.ChannelId;

            this.discordClient = new DiscordClient(cfg);

            this.discordClient.Ready += this.Client_Ready;
            this.discordClient.GuildAvailable += this.Client_GuildAvailable;
            this.discordClient.GuildUnavailable += this.Client_GuildUnavailable;
            this.discordClient.ClientErrored += this.Client_ClientError;

            this.discordClient.ChannelUpdated += this.Client_ChannelUpdated;

            this.discordClient.MessageCreated += messageHandler.Created;
            this.discordClient.MessageUpdated += messageHandler.Edited;
            this.discordClient.MessageDeleted += messageHandler.Deleted;
            this.discordClient.TypingStarted += messageHandler.Typing;

            webhookClient = new DiscordWebhookClient();
            await webhookClient.AddWebhookAsync(WebhookUri);

            foreach (DiscordWebhook webhook in webhookClient.Webhooks)
            {
                if (webhook.ChannelId == botConfig.ChannelId)
                {
                    WebhookChannelMatch = true;
                }
            }

            await this.discordClient.ConnectAsync();
            try
            {
                await Task.Delay(-1, stopClientTokenSource.Token);
            }
            finally
            {
                // Cancellation requested or the client crashed, perform cleanup.
                this.Ready = false;

                await this.discordClient.DisconnectAsync();
                this.discordClient.Dispose();
                this.Running = false;
                stopClientTokenSource.Dispose();
            }
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "WebChat", "Client is ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "WebChat", $"Guild available: {e.Guild.Name}", DateTime.Now);

            if (e.Guild.Id == this.GuildId)
            {
                this.Ready = true;

                _ = this.DownloadGuildLogo();
                StreamChannelName = e.Guild.GetChannel(ChannelId).Name;
            }

            return Task.CompletedTask;
        }

        private Task Client_GuildUnavailable(GuildDeleteEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Warning, "WebChat", $"Guild not available: {e.Guild.Name}", DateTime.Now);

            if (e.Guild.Id == this.GuildId)
            {
                this.Ready = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs an error to the console.
        /// </summary>
        /// <param name="e">The event holding the error.</param>
        /// <returns>The task representing the method's execution state.</returns>
        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "WebChat", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes a channel update and changes the displayed stream channel name if appropriate.
        /// </summary>
        /// <param name="e">The ChannelUpdateEventArgs event.</param>
        /// <returns>The task representing the method's execution state.</returns>
        private Task Client_ChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (e.ChannelAfter.Id == this.ChannelId)
            {
                this.StreamChannelName = e.ChannelAfter.Name;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to stop the bot.
        /// </summary>
        public void StopBot()
        {
            stopClientTokenSource.Cancel();
        }

        /// <summary>
        /// Fire this after receiving a message from the web bridge.
        /// Current actions supported: Send, Delete
        /// </summary>
        /// <param name="message">The message coming from the SignalR hub.</param>
        /// <returns>The task representing the message's processing status.</returns>
        public async Task IngestSignalR(BridgeMessage message)
        {
            if (!Ready)
            {
                throw new NullReferenceException("Guild not found.");
            }

            if (message.SignalRMessage.Action == "NewMessage")
            {
                DiscordChannel bridgeChannel = await this.discordClient.GetChannelAsync(ChannelId);
                DiscordMember guildMember = await bridgeChannel.Guild.GetMemberAsync(message.UserId);

                NewMessage newMessage = message.SignalRMessage as NewMessage;

                MatchCollection colonEmotes = Regex.Matches(newMessage.Content, @":[a-zA-Z0-9_~]+:(?!\d+)");
                string translatedContent = newMessage.Content;

                foreach (Match colonEmote in colonEmotes)
                {
                    try
                    {
                        DiscordEmoji emote = DiscordEmoji.FromName(discordClient, colonEmote.Value);
                        if (emote.Id == 0)
                        {
                            translatedContent = translatedContent.Replace(colonEmote.Value, emote.Name);
                        }
                        else if (emote.IsAnimated)
                        {
                            translatedContent = translatedContent.Replace(colonEmote.Value, $"<a:{emote.Name}:{emote.Id}>");
                        }
                        else if (!emote.IsAnimated)
                        {
                            translatedContent = translatedContent.Replace(colonEmote.Value, $"<:{emote.Name}:{emote.Id}>");
                        }
                    }
                    catch
                    {
                        // The emote doesn't exist on the target server, or it's been deleted.
                        // Just do nothing (don't attempt to translate it)
                    }
                }

                if (guildMember == null)
                {
                    throw new UnauthorizedAccessException("Not in Discord guild.");
                }
                else
                {
                    await webhookClient.Webhooks[0].ExecuteAsync(translatedContent, guildMember.DisplayName, guildMember.AvatarUrl, false, null, null);
                }
            }
            else if (message.SignalRMessage.Action == "DeleteMessage")
            {
                DiscordChannel bridgeChannel = await this.discordClient.GetChannelAsync(ChannelId);
                DiscordMember guildMember = await bridgeChannel.Guild.GetMemberAsync(message.UserId);

                if (guildMember == null)
                {
                    return;
                }

                if (bridgeChannel.PermissionsFor(guildMember).HasPermission(Permissions.ManageMessages))
                {
                    DiscordMessage delMessage = await bridgeChannel.GetMessageAsync(ulong.Parse(message.SignalRMessage.MessageId));
                    await delMessage.DeleteAsync();
                    return;
                }

                // Nothing to do, user doesn't have permission to delete the message.
                return;
            }
            else
            {
                throw new InvalidOperationException("Undefined action.");
            }
        }

        /// <summary>
        /// Attempts to get a Discord member by a given ulong.
        /// Throws a NullReferenceException if the member doesn't exist.
        /// </summary>
        /// <param name="id">The Discord user's ID</param>
        /// <returns>Returns a DiscordMember object</returns>
        /// <exception cref="NullReferenceException">Thrown if the member doesn't exist.</exception>
        public async Task<DiscordMember> GetMemberById(ulong id)
        {
            DiscordMember member = null;
            try
            {
                member = await (await this.discordClient.GetChannelAsync(ChannelId)).Guild.GetMemberAsync(id);
            }
            catch (DSharpPlus.Exceptions.NotFoundException) { /* Handled in null check below */ }
            if (member == null)
            {
                throw new NullReferenceException("Member does not exist.");
            }
            else
            {
                return member;
            }
        }

        /// <summary>
        /// Gets a Discord user by their ID.
        /// </summary>
        /// <param name="id">The ID to look up.</param>
        /// <returns>A DiscordUser object. May be null.</returns>
        public async Task<DiscordUser> GetUserById(ulong id)
        {
            return await discordClient.GetUserAsync(id);
        }

        public async Task<string> GetEmojiForEmojiMartAsync()
        {
            DiscordChannel streamChannel = await discordClient.GetChannelAsync(ChannelId);
            IReadOnlyList<DiscordEmoji> emotes = await streamChannel.Guild.GetEmojisAsync();

            if (string.IsNullOrEmpty(this.ShortServerName))
            {
                this.ShortServerName = streamChannel.Guild.Name.Split(" ")[0].ToLower();
            }

            List<Emoji> EmojiMartEmoji = new List<Emoji>();

            foreach (DiscordEmoji emote in emotes)
            {
                Emoji emoji = new Emoji
                {
                    // Hack to somehow get Discord mention string in the emote data
                    emoticons = new string[] { $":{emote.Name}:" },
                    name = $"{ShortServerName}_{emote.Name}",
                    colons = $"{ShortServerName}_{emote.Name}",
                    short_names = new string[] { $"{ShortServerName}_" + emote.Name },
                    imageUrl = emote.Url
                };

                EmojiMartEmoji.Add(emoji);
            }

            return JsonConvert.SerializeObject(EmojiMartEmoji);
        }

        /// <summary>
        /// Get the Discord username and discriminator of the current bot.
        /// </summary>
        /// <returns>The bot's username and discriminator, separated by a hash.</returns>
        public string GetClientUsername()
        {
            if (string.IsNullOrEmpty(ClientName))
            {
                ClientName = $"{discordClient.CurrentUser.Username}#{discordClient.CurrentUser.Discriminator}";
            }

            return ClientName;
        }

        /// <summary>
        /// Download the stream guild's logo and save it in the images folder.
        /// </summary>
        /// <returns>The task representing the download's state.</returns>
        public async Task DownloadGuildLogo()
        {
            if (!UseServerLogo)
            {
                return;
            }
            bool logoDownloadSuccess = false;
            HttpClient httpClient = new HttpClient();
            DiscordChannel chatChannel = await this.discordClient.GetChannelAsync(this.ChannelId);
            _LogoWebPath = $"/images/logo.{chatChannel.Guild.IconUrl.Split('.').Last()}";
            string LogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _LogoWebPath.Replace('/', Path.DirectorySeparatorChar));
            int i = 0;
            do
            {
                HttpResponseMessage logoResponse = await httpClient.GetAsync(chatChannel.Guild.IconUrl);

                if (logoResponse.IsSuccessStatusCode)
                {
                    using (FileStream fs1 = new FileStream(LogoPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await (await logoResponse.Content.ReadAsStreamAsync()).CopyToAsync(fs1);
                        await fs1.FlushAsync();
                    }
                    logoResponse.Dispose();
                    logoDownloadSuccess = true;
                }
                else
                {
                    logoResponse.Dispose();
                }
                i++;
                System.Threading.Thread.Sleep(5000);
                if (i > 5)
                {
                    throw new FileLoadException("Could not download guild logo.");
                }
            } while (!logoDownloadSuccess);
        }
    }
}
