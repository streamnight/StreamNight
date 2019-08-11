using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Status
{
    public class SystemStatus
    {
        private static System.Timers.Timer StatusUpdateTimer;

        private readonly Discord.Client discordClient;

        [System.Runtime.Serialization.IgnoreDataMember]
        public readonly IHubContext<StatusHub> statusHub;

        public SystemStatus(DiscordBot discordBot, IHubContext<StatusHub> statusHubContext)
        {
            statusHub = statusHubContext;

            discordClient = discordBot.DiscordClient;

            StatusUpdateTimer = new System.Timers.Timer(5000);
            StatusUpdateTimer.Elapsed += UpdateStatusFromEvent;
            StatusUpdateTimer.AutoReset = true;
            StatusUpdateTimer.Enabled = true;

            discordClient.SystemStatus = this;
        }

        private void UpdateStatusFromEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.UpdateStatus();
        }

        public event EventHandler StatusChanged;
        private bool RaiseStatusChanged;

        // Status information
        /// <summary>
        /// Last poll time, in Unix seconds from UTC.
        /// </summary>
        public long LastPoll { get; private set; }
        /// <summary>
        /// Last time a value changed, in Unix seconds from UTC.
        /// </summary>
        public long LastChange { get; private set; }

        // Stream information
        public bool StreamUp { get; private set; }
        public string StreamChannelName { get; private set; }
        public string StreamRoleName { get; private set; }
        public string AdminRoleName { get; private set; }
        public bool UsingServerIcon { get; private set; }

        // Bot information
        public bool BotRunning { get; private set; }
        public bool GuildReady { get; private set; }
        public bool WebhookAndChannelMatch { get; private set; }

        public void UpdateStatus()
        {
            if (StreamUp != discordClient.StreamUp || 
                StreamChannelName != discordClient.StreamChannelName ||
                StreamRoleName != discordClient.StreamRole ||
                AdminRoleName != discordClient.AdminRole ||
                UsingServerIcon != discordClient.UseServerLogo ||
                GuildReady != discordClient.Ready ||
                BotRunning != discordClient.Running ||
                WebhookAndChannelMatch != discordClient.WebhookChannelMatch)
            {
                this.RaiseStatusChanged = true;
            }

            StreamUp = discordClient.StreamUp;
            StreamChannelName = discordClient.StreamChannelName;
            StreamRoleName = discordClient.StreamRole;
            AdminRoleName = discordClient.AdminRole;

            UsingServerIcon = discordClient.UseServerLogo;

            GuildReady = discordClient.Ready;
            BotRunning = discordClient.Running;
            WebhookAndChannelMatch = discordClient.WebhookChannelMatch;

            LastPoll = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (this.RaiseStatusChanged)
            {
                LastChange = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                StatusChanged.Invoke(this, new EventArgs());
                this.RaiseStatusChanged = false;
            }
        }
    }
}
