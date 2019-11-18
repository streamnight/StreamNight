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
    public class PresenceModel : PageModel
    {
        public readonly Client DiscordClient;

        [BindProperty]
        public PresenceData PresenceData { get; set; }

        public string LogoPath;
        public string StreamName;

        public PresenceModel(DiscordBot discordBot)
        {
            DiscordClient = discordBot.DiscordClient;
            LogoPath = DiscordClient.LogoWebPath;
            StreamName = DiscordClient.StreamName;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["ResultMessage"] = "Invalid model state. Check the values and try again.";
                return BadRequest();
            }

            await this.DiscordClient.SetPresence(PresenceData);
            return Page();
        }
    }
}