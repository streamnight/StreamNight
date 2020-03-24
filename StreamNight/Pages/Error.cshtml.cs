using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreamNight.SupportLibs.Discord;

namespace StreamNight.Pages
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string ErrorMessage { get; set; } = string.Empty;

        public bool UserIsAdministrator { get; set; } = false;

        private readonly Client _discordClient;

        public string StreamName { get; set; }
        public string LogoPath { get; set; }


        public ErrorModel(DiscordBot discordBot)
        {
            _discordClient = discordBot.DiscordClient;
            StreamName = _discordClient.StreamName;
            LogoPath = _discordClient.LogoWebPath;
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (User.IsInRole("StreamController") || User.IsInRole("Administrator"))
            {
                UserIsAdministrator = true;

                if (!_discordClient.Ready && _discordClient.Running)
                {
                    ErrorMessage = "Couldn't connect to the Discord API.";
                }
                else if (!_discordClient.Running)
                {
                    ErrorMessage = "The Discord bot crashed.";
                }
                else
                {
                    ErrorMessage = "Error doesn't have a custom message, check the application logs for more information.";
                }
            }
        }
    }
}
