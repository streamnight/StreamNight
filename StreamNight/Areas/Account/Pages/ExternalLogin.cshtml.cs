using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StreamNight.Areas.Account.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace StreamNight.Areas.Account.Pages
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<StreamNightUser> _signInManager;
        private readonly UserManager<StreamNightUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly DiscordBot _discordBot;

        public ExternalLoginModel(
            SignInManager<StreamNightUser> signInManager,
            UserManager<StreamNightUser> userManager,
            ILogger<ExternalLoginModel> logger,
            DiscordBot discordBot)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _discordBot = discordBot;
            LogoPath = discordBot.DiscordClient.LogoWebPath;
            StreamName = discordBot.DiscordClient.StreamName;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string LoginProvider { get; set; }

        public string ReturnUrl { get; set; }

        public string LogoPath { get; set; }
        public string StreamName { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public ulong DiscordId { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                // Get the logged in user from the database
                StreamNightUser loggedInUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                try
                {
                    bool hasStreamRole = false;
                    bool hasAdminRole = false;
                    // Get the user's roles from the Discord bot
                    DSharpPlus.Entities.DiscordMember member = await _discordBot.DiscordClient.GetMemberById(loggedInUser.DiscordId);
                    foreach (DSharpPlus.Entities.DiscordRole discordRole in member.Roles)
                    {
                        // If the user has the stream role
                        if (discordRole.Name == _discordBot.DiscordClient.StreamRole)
                        {
                            hasStreamRole = true;
                        }
                        if (discordRole.Name == _discordBot.DiscordClient.AdminRole)
                        {
                            hasAdminRole = true;
                        }
                    }

                    // Check if the user is in the stream role already
                    bool userInStreamRole = await _userManager.IsInRoleAsync(loggedInUser, "StreamController");
                    if (!userInStreamRole && hasStreamRole)
                    {
                        // Add the stream role to the user
                        await _userManager.AddToRoleAsync(loggedInUser, "StreamController");

                        // Refresh the login cookie
                        await _signInManager.RefreshSignInAsync(loggedInUser);
                    }
                    else if (userInStreamRole && !hasStreamRole)
                    {
                        // Remove the stream role from the user
                        await _userManager.RemoveFromRoleAsync(loggedInUser, "StreamController");

                        // Refresh the login cookie
                        await _signInManager.RefreshSignInAsync(loggedInUser);
                    }

                    // Check if the user is in the admin role already
                    bool userInAdmin = await _userManager.IsInRoleAsync(loggedInUser, "Administrator");
                    if (!userInAdmin && hasAdminRole)
                    {
                        // Add the admin role to the user
                        await _userManager.AddToRoleAsync(loggedInUser, "Administrator");

                        // Refresh the login cookie
                        await _signInManager.RefreshSignInAsync(loggedInUser);
                    }
                    else if (userInAdmin && !hasAdminRole)
                    {
                        // Remove the admin role from the user
                        await _userManager.RemoveFromRoleAsync(loggedInUser, "Administrator");

                        // Refresh the login cookie
                        await _signInManager.RefreshSignInAsync(loggedInUser);
                    }
                }
                // The user might not exist, catch the exception and do nothing
                catch (NullReferenceException) { }

                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                LoginProvider = info.LoginProvider;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    ulong.TryParse(info.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out ulong dId);
                    Input = new InputModel
                    {
                        DiscordId = dId
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                ulong.TryParse(info.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out ulong DiscordId);
                var user = new StreamNightUser { UserName = DiscordId.ToString(), DiscordId = DiscordId };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    try
                    {
                        // Get the user's roles from the Discord bot
                        DSharpPlus.Entities.DiscordMember member = await _discordBot.DiscordClient.GetMemberById(user.DiscordId);
                        foreach (DSharpPlus.Entities.DiscordRole discordRole in member.Roles)
                        {
                            // If the user has the stream role
                            if (discordRole.Name == _discordBot.DiscordClient.StreamRole)
                            {
                                await _userManager.AddToRoleAsync(user, "StreamController");
                            }
                            if (discordRole.Name == _discordBot.DiscordClient.AdminRole)
                            {
                                await _userManager.AddToRoleAsync(user, "Administrator");
                            }
                        }
                    }
                    // The user might not exist, catch the exception and do nothing
                    catch (NullReferenceException) { }

                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                else if (result.Errors.Where(e => e.Code == "DuplicateUserName").Count() > 0)
                {
                    var existingUser = await _userManager.FindByNameAsync(DiscordId.ToString());
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);

                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                else
                {
                    ViewData["ResultMessage"] = "Couldn't sign in with Discord. Try logging in with a DM instead.";
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            LoginProvider = info.LoginProvider;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
