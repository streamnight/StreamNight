using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Discord
{
    public struct Config
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }

        [JsonProperty("guild")]
        public ulong GuildId { get; private set; }

        [JsonProperty("channel")]
        public ulong ChannelId { get; private set; }

        [JsonProperty("use_server_logo")]
        public bool UseServerLogo { get; private set; }

        [JsonProperty("stream_name")]
        public string StreamName { get; private set; }

        [JsonProperty("short_server_name")]
        public string ShortServerName { get; private set; }

        [JsonProperty("hmac_key")]
        public string HmacKey { get; private set; }

        [JsonProperty("api_url")]
        public string ApiUrl { get; private set; }

        [JsonProperty("token_key")]
        public string TokenKey { get; private set; }

        [JsonProperty("token_password")]
        public string TokenPassword { get; private set; }

        [JsonProperty("webhook_url")]
        public string WebhookUrl { get; private set; }

        [JsonProperty("stream_role")]
        public string StreamRole { get; private set; }
    }
}
