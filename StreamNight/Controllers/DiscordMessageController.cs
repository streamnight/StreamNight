using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs;
using StreamNight.SupportLibs.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace StreamNight.Controllers
{
    [Route("internal/[controller]")]
    [ApiController]
    public class DiscordMessageController : ControllerBase
    {
        private readonly IHubContext<BridgeHub> _bridgeContext;
        private readonly IConfiguration _configuration;

        public DiscordMessageController(IHubContext<BridgeHub> bridgeContext, IConfiguration configuration)
        {
            _bridgeContext = bridgeContext;
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a Discord message to clients connected to the SignalR hub.
        /// </summary>
        /// <param name="inputJson">The encrypted, signed NewMessage object as JSON.</param>
        /// <returns>Status message for the response. 401 Unauthorized if invalid HMAC, 400 BadRequest if invalid JSON, or 200 OK if successful.</returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult> NewDiscordMessage([FromBody] string inputJson)
        {
            if (!await Task.Run(() => Hmac.VerifyMessage(inputJson, _configuration["HmacKey"])))
            {
                return Unauthorized();
            }

            NewMessage message;
            try
            {
                string messageContent = Hmac.DecodeMessage(inputJson);
                message = JsonConvert.DeserializeObject<NewMessage>(messageContent);
            }
            catch
            {
                return BadRequest();
            }

            // Message is from Discord, just send it to the clients.
            await _bridgeContext.Clients.All.SendAsync(message.Action, message);
            return Ok();
        }

        /// <summary>
        /// Sends an edit notification to clients connected to the SignalR hub.
        /// </summary>
        /// <param name="inputJson">The encrypted, signed EditMessage object as JSON.</param>
        /// <returns>Status message for the response. 401 Unauthorized if invalid HMAC, 400 BadRequest if invalid JSON, or 200 OK if successful.</returns>
        [HttpPut]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult> EditDiscordMessage([FromBody] string inputJson)
        {
            if (!await Task.Run(() => Hmac.VerifyMessage(inputJson, _configuration["HmacKey"])))
            {
                return Unauthorized();
            }

            EditMessage message;
            try
            {
                string messageContent = Hmac.DecodeMessage(inputJson);
                message = JsonConvert.DeserializeObject<EditMessage>(messageContent);
            }
            catch
            {
                return BadRequest();
            }

            await _bridgeContext.Clients.All.SendAsync(message.Action, message);
            return Ok();
        }

        /// <summary>
        /// Sends a delete notification to clients connected to the SignalR hub.
        /// </summary>
        /// <param name="inputJson">The encrypted, signed DeleteMessage object as JSON.</param>
        /// <returns>Status message for the response. 401 Unauthorized if invalid HMAC, 400 BadRequest if invalid JSON, or 200 OK if successful.</returns>
        [HttpDelete]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult> DeleteDiscordMessage([FromBody] string inputJson)
        {
            if (!await Task.Run(() => Hmac.VerifyMessage(inputJson, _configuration["HmacKey"])))
            {
                return Unauthorized();
            }

            DeleteMessage message;
            try
            {
                string messageContent = Hmac.DecodeMessage(inputJson);
                message = JsonConvert.DeserializeObject<DeleteMessage>(messageContent);
            }
            catch
            {
                return BadRequest();
            }

            // Message deletion event from Discord, just send it to the clients.
            await _bridgeContext.Clients.All.SendAsync(message.Action, message);
            return Ok();
        }
    }
}