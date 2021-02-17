using StreamNight.SupportLibs;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Discord
{
    public class Commands
    {
        private readonly string TokenKey;
        private readonly string TokenPassword;
        private readonly string ApiUrl;
        private readonly ulong StreamChannel;
        private readonly string StreamRole;
        private Client Client;

        private MessageCreateEventArgs messageEvent;

        public Commands(MessageHandlerConfig config)
        {
            TokenKey = config.TokenKey;
            TokenPassword = config.TokenPassword;
            ApiUrl = config.ApiUrl;
            StreamChannel = config.ChannelId;
            Client = config.Client;
            StreamRole = config.StreamRole;
        }

        public async Task ProcessCommand(MessageCreateEventArgs messageEvent)
        {
            this.messageEvent = messageEvent;
            DiscordMessage message = messageEvent.Message;

            if (message.Content == "streambot!ping")
            {
                await Ping(message);
            }
            else if (message.Content == "streambot!login")
            {
                await RequestLogin(message);
            }
            else if (message.Content == "streambot!up")
            {
                if (CheckRole(message))
                {
                    Client.StreamUp = true;
                    await message.RespondAsync(embed: PresetEmbeds.SuccessEmbed("Stream marked as up.", message).Build());
                    await Client.MessageHandler.StreamUp();
                }
                else
                {
                    await message.RespondAsync(embed: PresetEmbeds.ErrorEmbed("Insufficient permissions.", message).Build());
                }
            }
            else if (message.Content == "streambot!down")
            {
                if (CheckRole(message))
                {
                    Client.StreamUp = false;
                    await message.RespondAsync(embed: PresetEmbeds.SuccessEmbed("Stream marked as down.", message).Build());
                }
                else
                {
                    await message.RespondAsync(embed: PresetEmbeds.ErrorEmbed("Insufficient permissions.", message).Build());
                }
            }
            else if (message.Content == "streambot!status")
            {
                await message.RespondAsync(embed: PresetEmbeds.InfoEmbed($"Stream running: {Client.StreamUp}", message).Build());
            }
            else if (message.Content.StartsWith("streambot!"))
            {
                await message.RespondAsync(embed: PresetEmbeds.ErrorEmbed("Use `streambot!login` in a DM to request a login link.", message).Build());
            }
        }

        private bool CheckRole(DiscordMessage message)
        {
            foreach (DiscordRole role in (message.Author as DiscordMember).Roles)
            {
                if (role.Name == StreamRole)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task RequestLogin(DiscordMessage message)
        {
            if (!message.Channel.IsPrivate)
            {
                await message.RespondAsync(embed: PresetEmbeds.ErrorEmbed("This command must be run in a DM.", message).Build());
            }
            else
            {
                DiscordGuild streamGuild = messageEvent.Guild.GetChannel(StreamChannel).Guild;
                if (await streamGuild.GetMemberAsync(message.Author.Id) == null)
                {
                    await message.RespondAsync(embed: PresetEmbeds.ErrorEmbed($"We can't find you in our guild. are you part of {streamGuild.Name}?", message).Build());
                    return;
                }

                string signedId = Hmac.SignMessage(message.Author.Id.ToString(), TokenKey);
                string encryptedId = StringCipher.Encrypt(signedId, TokenPassword);

                await message.RespondAsync(embed: PresetEmbeds.SuccessEmbed($"[Click here to login.]({ApiUrl}BotLogin?token={encryptedId})\n\n" +
                                                                            $"This link expires in 30 seconds.", message).Build());
            }
        }

        private async Task Ping(DiscordMessage message)
        {
            await message.RespondAsync("Pong, I'm awake!");
        }
    }
}
