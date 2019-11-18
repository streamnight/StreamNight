using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs.Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StreamNight.Controllers
{
    [Route("chat/[controller]")]
    [EnableCors("AllowAll")]
    [Authorize]
    [ApiController]
    public class EmojiController : ControllerBase
    {
        private readonly DiscordBot _discordBot;
        private readonly Client _discordClient;

        public EmojiController(DiscordBot discordBot)
        {
            _discordBot = discordBot;
            _discordClient = _discordBot.DiscordClient;
        }

        /// <summary>
        /// Returns JSON object containing server custom emotes for Emoji Mart.
        /// </summary>
        /// <returns>JSON object containing custom emotes for Emoji Mart.</returns>
        [HttpGet]
        public async Task<ActionResult<string>> GetEmoji()
        {
            // Can't get emotes if the Discord client isn't ready, so fail early instead.
            if (!_discordClient.Ready)
            {
                throw new ApplicationException("The Discord client isn't ready.");
            }

            // This function returns JSON so it's safe to pass directly to the client.
            return await _discordBot.DiscordClient.GetEmojiForEmojiMartAsync();
        }
    }
}