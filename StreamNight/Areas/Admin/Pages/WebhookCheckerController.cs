using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StreamNight.Areas.Admin.Pages
{
    [Authorize(Roles = "Administrator")]
    [Route("Admin/[controller]")]
    public class WebhookCheckerController : Controller
    {
        HttpClient httpClient = new HttpClient();

        // POST api/<controller>
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody]string value)
        {
            if (value.StartsWith("https://discord.com/api/webhooks/"))
            {
                Stream discordWebhookResponse = await httpClient.GetStreamAsync(value);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DiscordWebhookResponse));
                try
                {
                    return Ok((serializer.ReadObject(discordWebhookResponse) as DiscordWebhookResponse).channel_id);
                }
                catch
                {
                    return BadRequest();
                }
            }
            else
            {
                return BadRequest();
            }
        }
    }

    public class DiscordWebhookResponse
    {
        public string name { get; set; }
        public string channel_id { get; set; }
        public string token { get; set; }
        public string avatar { get; set; }
        public string guild_id { get; set; }
        public string id { get; set; }
    }
}
