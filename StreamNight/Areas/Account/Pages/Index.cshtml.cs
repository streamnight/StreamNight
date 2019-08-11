using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.SupportLibs.Discord;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StreamNight.Areas.Account.Pages
{
    public class IndexModel : PageModel
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

        private StreamMember _currentMember { get; set; }
        public string LogoPath { get; set; }
        public string StreamName { get; set; }
        public string BotName { get; set; }

        public IndexModel(DiscordBot discordBot)
        {
            _discordClient = discordBot.DiscordClient;
            LogoPath = _discordClient.LogoWebPath;
            StreamName = _discordClient.StreamName;
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
                BotName = _discordClient.GetClientUsername();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Couldn't retrieve the username of the login bot.", e);
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

        public void OnGet()
        {

        }
    }

    public class StreamMember
    {
        public bool IsMember { get; set; }
        public DiscordMember Member { get; set; }
        public string Username { get; set; }
        public string AvatarUri { get; set; }
    }
}