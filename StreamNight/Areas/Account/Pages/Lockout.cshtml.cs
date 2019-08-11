using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StreamNight.Areas.Account.Pages
{
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        public string LogoPath { get; set; }
        public string StreamName { get; set; }

        public LockoutModel(DiscordBot discordBot)
        {
            LogoPath = discordBot.DiscordClient.LogoWebPath;
            StreamName = discordBot.DiscordClient.StreamName;
        }

        public void OnGet()
        {

        }
    }
}
