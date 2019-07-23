using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Models
{
    public class Emoji
    {
        // Emoji selector from Discord
#pragma warning disable IDE1006 // Naming Styles
        public string id { get; set; }
        // Human-readable name
        public string name { get; set; }
        // Human-readable name with colons
        public string colons { get; set; }
        public string[] short_names { get; set; }
        public string text { get; set; }
        public string[] emoticons { get; set; }
        public string[] keywords { get; set; }
        // Emote URL from Discord
        public string imageUrl { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
