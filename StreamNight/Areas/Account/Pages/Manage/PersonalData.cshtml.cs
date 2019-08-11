using System.Threading.Tasks;
using StreamNight.Areas.Account.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace StreamNight.Areas.Account.Pages.Manage
{
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<StreamNightUser> _userManager;
        private readonly ILogger<PersonalDataModel> _logger;

        public string StreamName;
        public string LogoPath;

        public PersonalDataModel(
            UserManager<StreamNightUser> userManager,
            ILogger<PersonalDataModel> logger,
            DiscordBot discordBot)
        {
            _userManager = userManager;
            _logger = logger;
            StreamName = discordBot.DiscordClient.StreamName;
            LogoPath = discordBot.DiscordClient.LogoWebPath;
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            return Page();
        }
    }
}