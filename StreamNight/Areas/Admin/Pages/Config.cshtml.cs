using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreamNight.SupportLibs.Discord;

namespace StreamNight.Areas.Admin.Pages
{
    [Authorize(Roles = "Administrator")]
    public class ConfigModel : PageModel
    {
        private readonly Client _discordClient;

        [BindProperty]
        public BotConfig NewConfig { get; set; }
        public BotConfig CurrentBotConfig { get
            {
                return _discordClient.botConfig;
            } }

        public Dictionary<string, object> ConfigProperties { get
            {
                Dictionary<string, object> ConfigProperties = new Dictionary<string, object>();
                foreach (PropertyInfo propertyInfo in CurrentBotConfig.GetType().GetProperties())
                {
                    ConfigProperties.Add(propertyInfo.Name, propertyInfo.GetValue(this.CurrentBotConfig));
                }
                return ConfigProperties;
            } }

        public string LogoPath;
        public string StreamName;

        public ConfigModel(DiscordBot discordBot)
        {
            _discordClient = discordBot.DiscordClient;
            LogoPath = _discordClient.LogoWebPath;
            StreamName = _discordClient.StreamName;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["ResultMessage"] = "Invalid model.";
                return Page();
            }

            // Values are valid, proceed to write it to disk.
            using (StreamWriter configFile = System.IO.File.CreateText("botconfig.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(configFile, NewConfig);
            }

            _discordClient.StopBot();
            while (_discordClient.Running)
            {
                await Task.Delay(500);
            }
            _ = _discordClient.RunBotAsync();
            ViewData["ResultMessage"] = "Configuration saved. The bot is restarting.";

            return Page();
        }
    }
}