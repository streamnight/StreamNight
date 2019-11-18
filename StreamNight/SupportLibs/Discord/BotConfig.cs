using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Discord
{
    public class BotConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("guild")]
        public ulong GuildId { get; set; }

        [JsonProperty("channel")]
        public ulong ChannelId { get; set; }

        [JsonProperty("use_server_logo")]
        public bool UseServerLogo { get; set; }

        [JsonProperty("stream_name")]
        public string StreamName { get; set; }

        [JsonProperty("short_server_name")]
        public string ShortServerName { get; set; }

        [JsonProperty("api_url")]
        public string ApiUrl { get; set; }

        [JsonProperty("hmac_key")]
        public string HmacKey { get; set; }

        [JsonProperty("token_key")]
        public string TokenKey { get; set; }

        [JsonProperty("token_password")]
        public string TokenPassword { get; set; }

        [JsonProperty("webhook_url")]
        public string WebhookUrl { get; set; }

        [JsonProperty("stream_role")]
        public string StreamRole { get; set; }

        [JsonProperty("admin_role")]
        public string AdminRole { get; set; }

        [JsonProperty("enable_presence")]
        public bool EnablePresence { get; set; }

        [JsonProperty("presence_message")]
        public string PresenceMessage { get; set; }
    }
}
