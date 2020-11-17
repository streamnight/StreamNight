using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs.Discord;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreamNight.Areas.Account.Pages;

namespace StreamNight.Pages
{
    [Authorize]
    [EnableCors("AllowAll")]
    public class StreamModel : PageModel
    {
        private readonly Client _discordClient;
        public StreamMember currentMember
        {
            get
            {
                if (_currentMember == null)
                {
                    PopulateData().Wait();
                }
                return _currentMember;
            }
        }

        public string StreamName { get; set; }
        public string ChannelName { get; set; }
        public string ShortServerName { get; set; }
		public string LogoPath { get; set; }

        public bool TwitchEnabled;
        public bool RedirectPlaylist;
        public string RedirectTarget;
        public List<string> TwitchChannels;

        public PlayerOptions PlayerOptions;

        private StreamMember _currentMember { get; set; }

        public StreamModel(DiscordBot discordBot)
        {
            _discordClient = discordBot.DiscordClient;
            StreamName = _discordClient.StreamName;
            ChannelName = _discordClient.StreamChannelName;
            ShortServerName = _discordClient.ShortServerName;

            TwitchEnabled = _discordClient.TwitchEnabled;
            TwitchChannels = _discordClient.TwitchChannels;

            RedirectPlaylist = _discordClient.RedirectPlaylist;
            RedirectTarget = _discordClient.RedirectTarget;
			LogoPath = _discordClient.LogoWebPath;

            PlayerOptions = _discordClient.PlayerOptions;
        }

        public async Task PopulateData()
        {
            _currentMember = await GetMemberData(ulong.Parse(User.Identity.Name));
        }

        public async Task<StreamMember> GetMemberData(ulong Id)
        {
            StreamMember member;

            if (!_discordClient.Ready)
            {
                throw new ApplicationException("The Discord client isn't ready.");
            }

            try
            {
                member = new StreamMember
                {
                    IsMember = true,
                    Member = await _discordClient.GetMemberById(Id),
                };

                member.Username = $"{member.Member.Username}#{member.Member.Discriminator}";
                member.AvatarUri = member.Member.AvatarUrl;
            }
            catch
            {
                DiscordUser user = await _discordClient.GetUserById(Id);
                member = new StreamMember
                {
                    IsMember = false,
                    Username = $"{user.Username}#{user.Discriminator}",
                    AvatarUri = user.AvatarUrl
                };
            }

            return member;
        }

        public async Task<IActionResult> OnGet()
        {
            await PopulateData();
            if (_currentMember.IsMember)
            {
                return Page();
            }
            else
            {
                return Redirect("/Account/");
            }
        }
    }
}