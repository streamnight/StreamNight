using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.Areas.Account.Data;
using StreamNight.SupportLibs.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace StreamNight.Areas.Account.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<StreamNightUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly Client _discordClient;

        public string BotName;
        public string StreamName;

        public LoginModel(SignInManager<StreamNightUser> signInManager, ILogger<LoginModel> logger, DiscordBot discordBot)
        {
            _signInManager = signInManager;
            _logger = logger;
            _discordClient = discordBot.DiscordClient;
            LogoPath = _discordClient.LogoWebPath;
            StreamName = _discordClient.StreamName;
        }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }
        public string LogoPath { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            try
            {
                BotName = _discordClient.GetClientUsername();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Couldn't retrieve the username of the login bot, maybe the Discord client isn't ready.", e);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;

            return Page();
        }
    }
}