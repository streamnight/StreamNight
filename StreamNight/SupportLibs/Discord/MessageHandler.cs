using StreamNight.SupportLibs.SignalR;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.AspNetCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StreamNight.SupportLibs.Discord
{
    public class MessageHandlerConfig
    {
        public ulong ChannelId { get; set; }
        public string ApiUrl { get; set; }
        public string HmacKey { get; set; }
        public string TokenKey { get; set; }
        public string TokenPassword { get; set; }
        public string StreamRole { get; set; }
        public Client Client { get; set; }
    }

    public class MessageHandler
    {
        private CommsHandler commsHandler;
        private Commands commands;
        private MessageHandlerConfig config;

        private readonly ulong ChannelId;

        public MessageHandler(MessageHandlerConfig config)
        {
            ChannelId = config.ChannelId;
            commsHandler = new CommsHandler(config);
            commands = new Commands(config);
            this.config = config;
        }

        public async Task Created(MessageCreateEventArgs messageEvent)
        {
            if (messageEvent.Channel.Id == ChannelId)
            {
                // Build new message object
                NewMessage message = new NewMessage(messageEvent.Message);

                // Forward to SignalR
                Task<HttpResponseMessage> apiTask = commsHandler.SendObject(message);
                HttpResponseMessage response = null;
                try
                {
                    response = await apiTask;
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Couldn't send message to SignalR.", e);
                }

                // Add to history
                config.Client.historyStore.AddMessage(messageEvent.Message);
            }

            await commands.ProcessCommand(messageEvent);
        }

        public async Task Edited(MessageUpdateEventArgs messageEvent)
        {
            if (messageEvent.Channel.Id == ChannelId)
            {
                // Build new message object
                EditMessage message = new EditMessage(messageEvent.Message);

                // Forward to SignalR
                Task<HttpResponseMessage> apiTask = commsHandler.EditObject(message);
                HttpResponseMessage response = null;
                try
                {
                    response = await apiTask;
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Couldn't send message to SignalR.", e);
                }

                // Edit history
                config.Client.historyStore.EditMessage(messageEvent.Message);
            }
        }

        public async Task Deleted(MessageDeleteEventArgs messageEvent)
        {
            if (messageEvent.Channel.Id == ChannelId)
            {
                // Build new message object
                DeleteMessage message = new DeleteMessage(messageEvent.Message);

                // Forward to SignalR
                Task<HttpResponseMessage> apiTask = commsHandler.DeleteObject(message);
                HttpResponseMessage response = null;
                try
                {
                    response = await apiTask;
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Couldn't send message to SignalR.", e);
                }

                // Delete from history
                config.Client.historyStore.RemoveMessage(messageEvent.Message);
            }
        }

        public async Task Typing(TypingStartEventArgs typingEvent)
        {
            if (typingEvent.Channel.Id == ChannelId)
            {
                TypingMessage message = new TypingMessage(typingEvent);

                // Forward to SignalR
                Task<HttpResponseMessage> apiTask = commsHandler.SendTyping(message);
                HttpResponseMessage response = null;
                try
                {
                    response = await apiTask;
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Couldn't send message to SignalR.", e);
                }
            }
        }

        public async Task StreamUp()
        {
            Task<HttpResponseMessage> apiTask = commsHandler.SendStreamUp("StreamUp");
            HttpResponseMessage response = null;
            try
            {
                response = await apiTask;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Couldn't send message to SignalR.", e);
            }
        }
    }

    public class CommsHandler
    {
        private HttpClient httpClient = new HttpClient();

        private readonly string ApiUrl;
        private readonly string ClientNotifyUrl;
        private readonly string TypingNotifyUrl;
        private readonly string HmacKey;

        public CommsHandler(MessageHandlerConfig config)
        {
            ApiUrl = config.ApiUrl + "internal/DiscordMessage";
            ClientNotifyUrl = config.ApiUrl + "stream/MasterPlaylist";
            TypingNotifyUrl = config.ApiUrl + "internal/DiscordEvent";
            HmacKey = config.HmacKey;
        }

        public async Task<HttpResponseMessage> SendObject(object input)
        {
            string jsonObject = JsonConvert.SerializeObject(input);
            string signedMessage = Hmac.SignMessage(jsonObject, HmacKey);
            string escapedMessage = HttpUtility.JavaScriptStringEncode(signedMessage, true);

            StringContent stringContent = new StringContent($"{escapedMessage}", Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(ApiUrl, stringContent);
        }

        public async Task<HttpResponseMessage> SendStreamUp(object input)
        {
            string signedMessage = Hmac.SignMessage(input.ToString(), HmacKey);
            string escapedMessage = HttpUtility.JavaScriptStringEncode(signedMessage, true);

            StringContent stringContent = new StringContent($"{escapedMessage}", Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(ClientNotifyUrl, stringContent);
        }

        public async Task<HttpResponseMessage> SendTyping(object input)
        {
            string jsonObject = JsonConvert.SerializeObject(input);
            string signedMessage = Hmac.SignMessage(jsonObject, HmacKey);
            string escapedMessage = HttpUtility.JavaScriptStringEncode(signedMessage, true);

            StringContent stringContent = new StringContent($"{escapedMessage}", Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(TypingNotifyUrl, stringContent);
        }

        public async Task<HttpResponseMessage> DeleteObject(object input)
        {
            string jsonObject = JsonConvert.SerializeObject(input);
            string signedMessage = Hmac.SignMessage(jsonObject, HmacKey);
            string escapedMessage = HttpUtility.JavaScriptStringEncode(signedMessage, true);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent($"{escapedMessage}", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(ApiUrl)
            };

            return await httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> EditObject(object input)
        {
            string jsonObject = JsonConvert.SerializeObject(input);
            string signedMessage = Hmac.SignMessage(jsonObject, HmacKey);
            string escapedMessage = HttpUtility.JavaScriptStringEncode(signedMessage, true);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent($"{escapedMessage}", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri(ApiUrl)
            };

            return await httpClient.SendAsync(request);
        }
    }
}
