using StreamNight.SupportLibs.History;
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
using Microsoft.Extensions.Logging;

namespace StreamNight.SupportLibs.Discord
{
    // TODO: Split this into multiple classes.
    // A huge list of fields like this is really ugly.
    public class Client
    {
        private CancellationTokenSource stopClientTokenSource;

        internal BotConfig botConfig;
        private DiscordClient discordClient;
        private DiscordWebhookClient webhookClient;
        /// <summary>
        /// The message storage location that is used to send historical messages to connecting clients.
        /// </summary>
        public IHistoryStore HistoryStore { get; set; }

        // Putting this here to mimic singleton behaviour.
        /// <summary>
        /// The SystemStatus object tracking this client. Null until the status page is accessed.
        /// </summary>
        public SystemStatus SystemStatus { get; set; } = null;

        internal MessageHandler MessageHandler { get; set; }

        /// <summary>
        /// Signals whether playlist requests should be sent to another source.
        /// </summary>
        public bool RedirectPlaylist { get; set; } = false;
        /// <summary>
        /// The redirect target location.
        /// </summary>
        public string RedirectTarget { get; set; } = null;

        /// <summary>
        /// Signals whether the stream is up.
        /// </summary>
        public bool StreamUp { get; set; } = false;
        /// <summary>
        /// Signals whether the Discord client is running.
        /// </summary>
        public bool Running { get; set; } = false;
        /// <summary>
        /// Signals whether the Discord client has successfully connected to the guild.
        /// </summary>
        public bool Ready { get; set; } = false;
        /// <summary>
        /// Signals whether the webhook's channel matches the channel ID set in the config.
        /// </summary>
        public bool WebhookChannelMatch { get; set; } = false;
        /// <summary>
        /// Signals whether the server logo should be displayed in the mobile header instead of the static logo.
        /// </summary>
        public bool UseServerLogo { get; set; } = false;
        private ulong GuildId { get; set; } = 0;
        private ulong ChannelId { get; set; } = 0;
        private string ClientName { get; set; } = null;

        /// <summary>
        /// A one word version of the server name used for emoji and the stream tab name.
        /// </summary>
        public string ShortServerName { get; set; } = null;

        /// <summary>
        /// The string used to generate the ASP.NET page titles.
        /// </summary>
        public string StreamName { get; set; } = null;
        /// <summary>
        /// The stream channel's Discord name.
        /// </summary>
        public string StreamChannelName { get; set; } = null;

        public string StreamRole { get; set; } = null;
        public string AdminRole { get; set; } = null;

        // Twitch section
        /// <summary>
        /// Signals whether Twitch integration is enabled.
        /// </summary>
        public bool TwitchEnabled { get; set; } = false;
        /// <summary>
        /// The list of Twitch channels displayed on the stream website.
        /// </summary>
        public List<string> TwitchChannels { get; set; } = new List<string>();
        // Presence Section
        public bool EnablePresence
        {
            get
            {
                try
                {
                    return !string.IsNullOrWhiteSpace(discordClient.CurrentUser.Presence.Activity.Name);
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }
        }
        public string PresenceMessage { get; set; } = null;
        public PresenceData CurrentPresence { get
            {
                try
                {
                    return new PresenceData
                    {
                        PresenceEnabled = this.EnablePresence,
                        PresenceMessage = this.discordClient.CurrentUser.Presence.Activity.Name,
                        ActivityType = this.discordClient.CurrentUser.Presence.Activity.ActivityType,
                        TwitchUrl = this.discordClient.CurrentUser.Presence.Activity.StreamUrl
                    };
                }
                catch (NullReferenceException)
                {
                    return new PresenceData
                    {
                        PresenceEnabled = false,
                        PresenceMessage = null,
                        ActivityType = ActivityType.Playing,
                        TwitchUrl = null
                    };
                }
            } }

        /// <summary>
        /// The video player's options
        /// </summary>
        public PlayerOptions PlayerOptions { get; set; } = new PlayerOptions();

        public string LogoWebPath
        {
            get
            {
                if (this.UseServerLogo)
                {
                    if (this.Ready && (DateTime.UtcNow - LastLogoRefresh).TotalMinutes > 60.0)
                    {
                        _ = this.DownloadGuildLogo();
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
        private DateTimeOffset LastLogoRefresh { get; set; } = DateTimeOffset.UnixEpoch;

        public Client(IHistoryStore historyStore)
        {
            this.HistoryStore = historyStore;
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
                MinimumLogLevel = LogLevel.Debug
            };

            MessageHandler = new MessageHandler(new MessageHandlerConfig
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

            this.discordClient.MessageCreated += MessageHandler.Created;
            this.discordClient.MessageUpdated += MessageHandler.Edited;
            this.discordClient.MessageDeleted += MessageHandler.Deleted;
            this.discordClient.TypingStarted += MessageHandler.Typing;

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

                this.discordClient.Dispose();
                this.Running = false;
                stopClientTokenSource.Dispose();
            }
        }

        private async Task Client_Ready(DiscordClient client, ReadyEventArgs e)
        {
            client.Logger.LogInformation("Client is ready to process events.", DateTime.Now);

            await this.SetPresence(new PresenceData
            {
                PresenceEnabled = this.botConfig.EnablePresence,
                PresenceMessage = this.botConfig.PresenceMessage
            });

            client.Logger.LogInformation("Updated presence from configuration.", DateTime.Now);
        }

        private Task Client_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            client.Logger.LogInformation($"Guild available: {e.Guild.Name}", DateTime.Now);

            if (e.Guild.Id == this.GuildId)
            {
                this.Ready = true;

                _ = this.DownloadGuildLogo();
                StreamChannelName = e.Guild.GetChannel(ChannelId).Name;
            }

            return Task.CompletedTask;
        }

        private Task Client_GuildUnavailable(DiscordClient client, GuildDeleteEventArgs e)
        {
            client.Logger.LogWarning($"Guild not available: {e.Guild.Name}", DateTime.Now);

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
        private Task Client_ClientError(DiscordClient client, ClientErrorEventArgs e)
        {
            client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes a channel update and changes the displayed stream channel name if appropriate.
        /// </summary>
        /// <param name="e">The ChannelUpdateEventArgs event.</param>
        /// <returns>The task representing the method's execution state.</returns>
        private Task Client_ChannelUpdated(DiscordClient client, ChannelUpdateEventArgs e)
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
                List<string> translatedEmotes = new List<string>(colonEmotes.Count);

                foreach (Match colonEmote in colonEmotes)
                {
                    if (translatedEmotes.Contains(colonEmote.Value))
                    {
                        break;
                    }

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

                        translatedEmotes.Add(colonEmote.Value);
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
                    await webhookClient.Webhooks[0].ExecuteAsync(new DiscordWebhookBuilder()
                    {
                        Content = translatedContent,
                        Username = guildMember.DisplayName,
                        AvatarUrl = guildMember.AvatarUrl,
                        IsTTS = false
                    });
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
            using HttpClient httpClient = new HttpClient();
            DiscordChannel chatChannel = await this.discordClient.GetChannelAsync(this.ChannelId);
            _LogoWebPath = $"/images/logo.{chatChannel.Guild.IconUrl.Split('.').Last()}";
            string currentDir = Directory.GetCurrentDirectory();
            string LogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _LogoWebPath.Remove(0, 1).Replace('/', Path.DirectorySeparatorChar));
            int i = 0;
            do
            {
                using (HttpResponseMessage logoResponse = await httpClient.GetAsync(chatChannel.Guild.IconUrl))
                {
                    if (logoResponse.IsSuccessStatusCode)
                    {
                        using (FileStream fs1 = new FileStream(LogoPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true))
                        {
                            await (await logoResponse.Content.ReadAsStreamAsync()).CopyToAsync(fs1);
                            await fs1.FlushAsync();
                        }
                        logoDownloadSuccess = true;
                        LastLogoRefresh = DateTimeOffset.UtcNow;
                        break;
                    }
                }
                i++;
                Thread.Sleep(5000);
                if (i > 5)
                {
                    throw new FileLoadException("Could not download guild logo.");
                }
            } while (!logoDownloadSuccess);
        }

        /// <summary>
        /// Sets the Discord presence held by the bot.
        /// </summary>
        /// <param name="presenceData">The presence data to use.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task SetPresence(PresenceData presenceData)
        {
            DiscordActivity discordActivity;

            if (presenceData.PresenceEnabled)
            {
                discordActivity = new DiscordActivity(presenceData.PresenceMessage, presenceData.ActivityType);

                if (presenceData.ActivityType == ActivityType.Streaming)
                {
                    discordActivity.StreamUrl = presenceData.TwitchUrl;
                }
            }
            else
            {
                discordActivity = new DiscordActivity();
            }

            await discordClient.UpdateStatusAsync(discordActivity);
        }
    }

    public class PresenceData
    {
        public bool PresenceEnabled { get; set; } = false;
        public string PresenceMessage { get; set; } = string.Empty;
        public ActivityType ActivityType { get; set; } = ActivityType.Playing;
        public string TwitchUrl { get; set; } = string.Empty;
    }

    public class PlayerOptions
    {
        public PlayerLocation Location { get; set; }
        public enum PlayerLocation
        {
            Top,
            Center
        }
    }

    public static class UserLogger
    {
        private static Task LogWriter;
        private static Queue<string> LogQueue = new Queue<string>();

        public enum ConnectionStatus
        {
            Connected,
            Disconnected
        }

        private static async Task WriteLogs()
        {
            if (LogWriter != null 
                && LogWriter.Id != Task.CurrentId
                && LogWriter.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException("Attempted to run multiple log writers.");
            }

            if (!Directory.Exists("UserLogs"))
            {
                Directory.CreateDirectory("UserLogs");
            }

            StreamWriter streamWriter = new StreamWriter(new FileStream("UserLogs" + Path.DirectorySeparatorChar + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), FileMode.Append, FileAccess.Write));

            while (LogQueue.Count > 0)
            {
                await streamWriter.WriteAsync(LogQueue.Dequeue());
            }

            await streamWriter.FlushAsync();
            streamWriter.Close();
        }

        public static void WriteLog(string userId, string connectionId, ConnectionStatus connectionStatus)
        {
            string userAction = connectionStatus switch
            {
                ConnectionStatus.Connected => "CONN",
                ConnectionStatus.Disconnected => "DISCONN",
                _ => throw new ArgumentException(nameof(connectionStatus))
            };

            LogQueue.Enqueue($"[{DateTimeOffset.UtcNow.ToUnixTimeSeconds()};{userId};{connectionId};{userAction}]\n");

            // Ugly but needs it because it's a nullable bool
            if (LogWriter?.IsCompleted != false)
            {
                LogWriter = Task.Run(WriteLogs);
            }
        }
    }
}
