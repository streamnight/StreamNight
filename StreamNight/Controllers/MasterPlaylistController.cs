using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs;
using StreamNight.SupportLibs.Discord;
using StreamNight.SupportLibs.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace StreamNight.Controllers
{
    [Route("stream/[controller]")]
    [ApiController]
    public class MasterPlaylistController : ControllerBase
    {
        private Client _client;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<BridgeHub> _bridgeContext;

        public MasterPlaylistController(IHubContext<BridgeHub> bridgeContext, DiscordBot discordBot, IConfiguration configuration)
        {
            _bridgeContext = bridgeContext;
            _client = discordBot.DiscordClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Retrieves master playlist and sends it to the client.
        /// </summary>
        /// <returns>An ActionResult containing the HLS playlist text as "application/x-mpegURL".</returns>
        [Authorize]
        [HttpGet]
        [HttpHead]
        public async Task<IActionResult> GetMasterPlaylist()
        {
            if (!_client.StreamUp)
            {
                return NotFound();
            }
            else
            {
                string playlistPath = System.IO.Path.GetFullPath(_configuration["PlaylistPath"]);
                if (System.IO.File.Exists(playlistPath))
                {
                    return File(await System.IO.File.ReadAllBytesAsync(playlistPath), "application/x-mpegURL");
                }
                else
                {
                    throw new FileNotFoundException("Playlist file not found.");
                }
            }
        }

        /// <summary>
        /// Sends StreamUp message to clients. Used to link Discord and the SignalR hub together.
        /// </summary>
        /// <param name="inputJson">Signed and encrypted JSON object with message payload.</param>
        /// <returns>Result of the performed action. 200 OK if processed correctly or 400 BadRequest if incorrect parameters.</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult> NewDiscordMessage([FromBody] string inputJson)
        {
            if (!await Task.Run(() => Hmac.VerifyMessage(inputJson, _configuration["HmacKey"])))
            {
                return Unauthorized();
            }

            try
            {
                string messageContent = Hmac.DecodeMessage(inputJson);
                if (messageContent == "StreamUp")
                {
                    // Signal stream up.
                    await _bridgeContext.Clients.All.SendAsync(messageContent);
                    return Ok();
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
        }
    }
}