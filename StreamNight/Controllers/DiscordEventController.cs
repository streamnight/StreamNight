using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs;
using StreamNight.SupportLibs.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace StreamNight.Controllers
{
    [Route("internal/[controller]")]
    [ApiController]
    public class DiscordEventController : Controller
    {
        private readonly IHubContext<BridgeHub> _bridgeContext;
        private readonly IConfiguration _configuration;

        public DiscordEventController(IHubContext<BridgeHub> bridgeContext, IConfiguration configuration)
        {
            _bridgeContext = bridgeContext;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SendClientTyping([FromBody] string inputJson)
        {
            if (!await Task.Run(() => Hmac.VerifyMessage(inputJson, _configuration["HmacKey"])))
            {
                return Unauthorized();
            }

            TypingMessage message;
            try
            {
                string messageContent = Hmac.DecodeMessage(inputJson);
                message = JsonConvert.DeserializeObject<TypingMessage>(messageContent);
            }
            catch
            {
                return BadRequest();
            }

            // Message is from Discord, just send it to the clients.
            await _bridgeContext.Clients.All.SendAsync("ClientTyping", message);
            return Ok();
        }
    }
}