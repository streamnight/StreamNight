using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StreamNight.SupportLibs.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Status
{
    /// <summary>
    /// A secure SignalR hub used to control Twitch integration.
    /// </summary>
    [Authorize(Roles = "StreamController,Administrator")]
    public class TwitchHub : Hub
    {
        private readonly Client discordClient;
        private readonly TwitchStatus twitchStatus;

        public TwitchHub(DiscordBot discordBot)
        {
            discordClient = discordBot.DiscordClient;
            twitchStatus = new TwitchStatus(discordClient);
        }

        /// <summary>
        /// Sends the current Twitch integration status to the calling client.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task GetTwitchStatus()
        {
            await Clients.Caller.SendAsync("TwitchStatus", twitchStatus);
        }

        /// <summary>
        /// Sends the current Twitch integration status to all clients.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        private async Task SendTwitchStatus()
        {
            await Clients.All.SendAsync("TwitchStatus", twitchStatus);
        }

        /// <summary>
        /// Adds a Twitch channel to the displayed list.
        /// </summary>
        /// <param name="channelName">The name of the Twitch channel.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AddChannel(string channelName)
        {
            if (discordClient.TwitchChannels.Contains(channelName))
            {
                await Clients.Caller.SendAsync("Error", "Channel is already in list.");
            }
            else
            {
                discordClient.TwitchChannels.Add(channelName);
                await SendTwitchStatus();
            }
        }

        /// <summary>
        /// Removes a Twitch channel from the displayed list.
        /// </summary>
        /// <param name="channelName">The name of the Twitch channel.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task RemoveChannel(string channelName)
        {
            if (!discordClient.TwitchChannels.Contains(channelName))
            {
                await Clients.Caller.SendAsync("Error", "Channel is not in list.");
            }
            else
            {
                discordClient.TwitchChannels.Remove(channelName);
                await SendTwitchStatus();
            }
        }

        /// <summary>
        /// Enables Twitch integration.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task EnableTwitch()
        {
            if (discordClient.TwitchEnabled)
            {
                await Clients.Caller.SendAsync("Failed");
                return;
            }
            discordClient.TwitchEnabled = true;
            await SendTwitchStatus();
        }

        /// <summary>
        /// Disables Twitch integration.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task DisableTwitch()
        {
            if (!discordClient.TwitchEnabled)
            {
                await Clients.Caller.SendAsync("Failed");
                return;
            }
            discordClient.TwitchEnabled = false;
            await SendTwitchStatus();
        }
    }

    public class TwitchStatus
    {
        private Client discordClient;

        public TwitchStatus(Client discordClient)
        {
            this.discordClient = discordClient;
        }

        public bool TwitchEnabled
        {
            get
            {
                return discordClient.TwitchEnabled;
            }
        }

        /// <summary>
        /// The list of currently displayed channels.
        /// </summary>
        public List<string> ChannelNames
        {
            get
            {
                return discordClient.TwitchChannels;
            }
        }
    }
}
