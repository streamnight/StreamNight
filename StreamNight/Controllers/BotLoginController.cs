using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.Areas.Identity.Data;
using StreamNight.Areas.Identity.Pages.Account;
using StreamNight.SupportLibs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StreamNight.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BotLoginController : ControllerBase
    {
        private readonly UserManager<StreamNightUser> _userManager;
        private readonly SignInManager<StreamNightUser> _signInManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordBot _discordBot;

        private const string returnUrl = "/";

        public BotLoginController(UserManager<StreamNightUser> userManager,
                                  SignInManager<StreamNightUser> signInManager, 
                                  ILogger<ExternalLoginModel> logger,
                                  IConfiguration configuration,
                                  DiscordBot discordBot)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
            _discordBot = discordBot;
        }

        [HttpGet]
        public async Task<IActionResult> LoginWithBot([FromQuery] string token)
        {
            string DiscordIdFromLink;
            bool isValid;

            // Validate the token.
            try
            {
                string decryptedToken = StringCipher.Decrypt(token, _configuration["TokenPassword"]);
                if (Hmac.VerifyMessage(decryptedToken, _configuration["TokenKey"]))
                {
                    isValid = true;
                    DiscordIdFromLink = Hmac.DecodeMessage(decryptedToken);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return BadRequest();
            }

            // Handle login.
            if (isValid)
            {
                StreamNightUser user = await _userManager.FindByNameAsync(DiscordIdFromLink);
                bool signInUser = true;

                if (user == null)
                {
                    signInUser = false;
                    // User doesn't have an account, let's transparently create one and log them in.
                    ulong.TryParse(DiscordIdFromLink, out ulong DiscordId);
                    user = new StreamNightUser { UserName = DiscordId.ToString(), DiscordId = DiscordId };
                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        signInUser = true;
                        _logger.LogInformation("Created a new user from a Discord link.");
                    }
                }

                if (signInUser)
                {
                    // Check if the user should be in the administrator role
                    try
                    {
                        bool hasAdminRole = false;
                        // Get the user's roles from the Discord bot
                        DSharpPlus.Entities.DiscordMember member = await _discordBot.DiscordClient.GetMemberById(user.DiscordId);
                        foreach (DSharpPlus.Entities.DiscordRole discordRole in member.Roles)
                        {
                            // If the user has the stream role
                            if (discordRole.Name == _discordBot.DiscordClient.StreamRole)
                            {
                                hasAdminRole = true;
                                break;
                            }
                        }

                        // Check if the user is in the admin role already
                        bool userInAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
                        if (!userInAdmin && hasAdminRole)
                        {
                            // Add the admin role to the user
                            await _userManager.AddToRoleAsync(user, "Administrator");
                        }
                        else if (userInAdmin && !hasAdminRole)
                        {
                            // Remove the admin role from the user
                            await _userManager.RemoveFromRoleAsync(user, "Administrator");
                        }
                    }
                    // The user might not exist, catch the exception and do nothing
                    catch (NullReferenceException) { }

                    await _signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: "DiscordLink");
                    _logger.LogInformation("User logged in using a Discord link.");
                }

                return LocalRedirect(returnUrl);
            }

            return BadRequest();
        }
    }
}