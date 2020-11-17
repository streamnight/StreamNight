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
    public class PlayerModel : PageModel
    {
        public readonly Client DiscordClient;

        [BindProperty]
        public PlayerOptions PlayerOptions { get; set; }

        public string LogoPath;
        public string StreamName;

        public PlayerModel(DiscordBot discordBot)
        {
            DiscordClient = discordBot.DiscordClient;
            LogoPath = DiscordClient.LogoWebPath;
            StreamName = DiscordClient.StreamName;
        }

        public void OnGet()
        {
            PlayerOptions = DiscordClient.PlayerOptions;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                ViewData["ResultMessage"] = "Invalid model state. Check the values and try again.";
                return BadRequest();
            }

            this.DiscordClient.PlayerOptions = PlayerOptions;
            return Page();
        }
    }
}
