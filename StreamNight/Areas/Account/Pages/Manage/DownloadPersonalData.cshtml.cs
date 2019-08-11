using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamNight.Areas.Account.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace StreamNight.Areas.Account.Pages.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<StreamNightUser> _userManager;
        private readonly ILogger<DownloadPersonalDataModel> _logger;

        public string StreamName;
        public string LogoPath;

        public DownloadPersonalDataModel(
            UserManager<StreamNightUser> userManager,
            ILogger<DownloadPersonalDataModel> logger,
            DiscordBot discordBot)
        {
            _userManager = userManager;
            _logger = logger;
            StreamName = discordBot.DiscordClient.StreamName;
            LogoPath = discordBot.DiscordClient.LogoWebPath;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("User with ID '{UserId}' asked for their personal data.", _userManager.GetUserId(User));

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(StreamNightUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(personalData)), "text/json");
        }
    }
}
