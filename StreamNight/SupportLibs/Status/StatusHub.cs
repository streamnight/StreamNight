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

            if (discordBot.DiscordClient.SystemStatus == null)
            {
                SystemStatus = new SystemStatus(discordBot, statusHubContext);
            }
            else
            {
                SystemStatus = discordBot.DiscordClient.SystemStatus;
            }
            SystemStatus.StatusChanged += this.UpdateStatus;
        }

        public async Task GetSystemStatus()
        {
            SystemStatus.UpdateStatus();

            await Clients.Caller.SendAsync("SystemStatus", SystemStatus);
        }

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
            await Clients.All.SendAsync("SystemStatus", SystemStatus);
        }

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
            await Clients.All.SendAsync("SystemStatus", SystemStatus);
        }

        public Task ToggleServerIcon()
        {
            if (discordClient.UseServerLogo)
            {
                discordClient.UseServerLogo = false;
            }
            else
            {
                discordClient.UseServerLogo = true;
            }

            return Task.CompletedTask;
        }

        private async void UpdateStatus(object sender, EventArgs e)
        {
            await (sender as SystemStatus).statusHub.Clients.All.SendAsync("SystemStatus", SystemStatus);
        }
    }
}
