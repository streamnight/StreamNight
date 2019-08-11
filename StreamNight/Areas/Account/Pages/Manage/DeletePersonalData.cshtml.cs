using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using StreamNight.Areas.Account.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace StreamNight.Areas.Account.Pages.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<StreamNightUser> _userManager;
        private readonly SignInManager<StreamNightUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;

        public string StreamName;
        public string LogoPath;

        public DeletePersonalDataModel(
            UserManager<StreamNightUser> userManager,
            SignInManager<StreamNightUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            DiscordBot discordBot)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            StreamName = discordBot.DiscordClient.StreamName;
            LogoPath = discordBot.DiscordClient.LogoWebPath;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Password not correct.");
                    return Page();
                }
            }

            var result = await _userManager.DeleteAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred deleteing user with ID '{userId}'.");
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            return Redirect("~/");
        }
    }
}