using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Status
{
    [Authorize(Roles = "StreamController,Administrator")]
    public class StatusHub : Hub
    {
        private readonly SystemStatus SystemStatus;
        private readonly Discord.Client discordClient;

        public StatusHub(DiscordBot discordBot, IHubContext<StatusHub> statusHubContext)
        {
            discordClient = discordBot.DiscordClient;

            // Check if DiscordClient already has a SystemStatus object
            if (discordBot.DiscordClient.SystemStatus == null)
            {
                SystemStatus = new SystemStatus(discordBot, statusHubContext);
                // SystemStatus constructor already sets DiscordClient property to itself, so no need to set it manually.
            }
            else
            {
                // Use the existing object.
                SystemStatus = discordBot.DiscordClient.SystemStatus;
            }
            // Subscribe to event.
            SystemStatus.StatusChanged += this.UpdateStatus;
        }

        /// <summary>
        /// Sends system status object to caller.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task GetSystemStatus()
        {
            SystemStatus.UpdateStatus();

            await Clients.Caller.SendAsync("SystemStatus", SystemStatus.StatusProperties);
        }

        /// <summary>
        /// Checks bot state and sends shutdown request.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task StopBot()
        {
            if (discordClient.Running)
            {
                discordClient.StopBot();
            }
            else
            {
                await Clients.Caller.SendAsync("Failed");
            }
            await Clients.All.SendAsync("SystemStatus", SystemStatus.StatusProperties);
        }

        /// <summary>
        /// Checks bot state and starts bot on a background thread.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task StartBot()
        {
            if (!discordClient.Running)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                discordClient.RunBotAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                await Clients.Caller.SendAsync("Failed");
            }
            await Clients.All.SendAsync("SystemStatus", SystemStatus.StatusProperties);
        }

        /// <summary>
        /// Toggles the server icon boolean on the current Discord client.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public Task ToggleServerIcon()
        {
            discordClient.UseServerLogo = !discordClient.UseServerLogo;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles the playlist redirect boolean on the current Discord client.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public Task TogglePlaylistRedirect()
        {
            discordClient.RedirectPlaylist = !discordClient.RedirectPlaylist;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the playlist redirect URL of the current Discord client to the input string.
        /// </summary>
        /// <param name="redirectUrl"></param>
        /// <returns></returns>
        public async Task SetRedirectTarget(string redirectUrl)
        {
            try
            {
                Uri redirectUri = new Uri(redirectUrl);
                discordClient.RedirectTarget = redirectUri.ToString();
            }
            catch
            {
                await this.Clients.Caller.SendAsync("Failed");
            }
        }

        /// <summary>
        /// Validates the client state, then sets the Discord presence of the client using the provided object.
        /// </summary>
        /// <param name="presenceData">The PresenceData object representing the desired state.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task UpdatePresenceAsync(Discord.PresenceData presenceData)
        {
            await this.discordClient.SetPresence(presenceData);
        }

        /// <summary>
        /// Callback function for status update event. Sends system status to all connected clients.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private async void UpdateStatus(object sender, EventArgs e)
        {
            await (sender as SystemStatus).statusHub.Clients.All.SendAsync("SystemStatus", SystemStatus.StatusProperties);
        }
    }
}
