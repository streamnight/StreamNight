using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreamNight.SupportLibs.Discord;

namespace StreamNight.Areas.Admin.Pages
{
    [Authorize(Roles = "StreamController,Administrator")]
    public class StatusModel : PageModel
    {
        public readonly Client DiscordClient;

        public string LogoPath;
        public string StreamName;

        public StatusModel(DiscordBot discordBot)
        {
            DiscordClient = discordBot.DiscordClient;
            LogoPath = DiscordClient.LogoWebPath;
            StreamName = DiscordClient.StreamName;
        }

        public void OnGet()
        {
        }
    }
}