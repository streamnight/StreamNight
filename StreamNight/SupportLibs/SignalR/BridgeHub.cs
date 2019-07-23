using StreamNight.SupportLibs.Models;
using StreamNight.SupportLibs.SignalR.Client;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.SignalR
{
    // Requires web login.
    [Authorize]
    [EnableCors("AllowAll")]
    public class BridgeHub : Hub
    {
        private readonly DiscordBot _discordBot;

        public BridgeHub(DiscordBot discordBot)
        {
            _discordBot = discordBot;
        }

        // This controller only expects messages from clients; the Discord bridge posts to
        // DiscordMessageController.cs
        public async Task SendMessage(SendMessage message)
        {
            NewMessage newMessage = new NewMessage(RemoveMassPings(message.Content));
            HubUser hubUser = UserHandler.UserMappings[this.Context.ConnectionId];

            BridgeMessage bridgeMessage = new BridgeMessage();
            ulong.TryParse(this.Context.User.Identity.Name, out ulong userId);
            bridgeMessage.UserId = userId;

            bridgeMessage.SignalRMessage = newMessage;

            await _discordBot.DiscordClient.IngestSignalR(bridgeMessage);
            await this.Clients.Others.SendAsync("StopTypingForClient", hubUser);
        }

        // No need to authorize it here because it already checks for the Manage Messages
        // permission in the Discord side.
        public async Task DeleteMessage(DeleteRequest message)
        {
            DeleteMessage deleteMessage = new DeleteMessage(message.Id);

            BridgeMessage bridgeMessage = new BridgeMessage();
            ulong.TryParse(this.Context.User.Identity.Name, out ulong userId);
            bridgeMessage.UserId = userId;

            bridgeMessage.SignalRMessage = deleteMessage;

            await _discordBot.DiscordClient.IngestSignalR(bridgeMessage);
        }

        public async Task NotifyOldBrowser()
        {
            await Task.Run(() => { UserHandler.UserMappings[Context.ConnectionId].IsOldBrowser = true; });
            return;
        }

        public async Task GetHistory()
        {
            object messageHistory = new
            {
                HistoryContent = _discordBot.DiscordClient.historyStore.GetMessagesAsNewMessage()
            };

            // Send history to client
            await this.Clients.Caller.SendAsync("MessageHistory", messageHistory);
        }

        public async Task ClientTyping()
        {
            HubUser hubUser = UserHandler.UserMappings[this.Context.ConnectionId];
            TypingMessage typingMessage = new TypingMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                UserId = hubUser.DiscordId
            };

            await this.Clients.Others.SendAsync("ClientTyping", typingMessage);
        }

        public async Task GetConnectedUsers()
        {
            List<HubUser> hubUsers = new List<HubUser>();
            foreach (KeyValuePair<string, HubUser> keyValuePair in UserHandler.UserMappings)
            {
                // Get the HubUser object that's mapped to the user's connection ID
                HubUser user = keyValuePair.Value;

                if (user.HasDiscordInfo == null)
                {
                    // Try to get a DiscordMember object from Discord.
                    // Only attempts it on the first go (i.e. if it hasn't looked for one before)
                    DiscordMember member = null;
                    try
                    {
                        // This isn't the default DiscordClient GetMemberById, look in Client.cs for more info.
                        // It throws an exception if it doesn't exist.
                        member = _discordBot.DiscordClient.GetMemberById(ulong.Parse(user.AspNetUsername)).Result;

                        // If the user is a member of the guild
                        user.HasDiscordInfo = true;
                        user.DiscordId = member.Id.ToString();
                        user.DiscordDisplayName = member.DisplayName;
                        // Replace GIF avatars with the first frame
                        user.AvatarUrl = member.AvatarUrl.Replace(".gif", ".png");
                    }
                    catch
                    {
                        // The user doesn't exist, stop the bot from searching again.
                        user.HasDiscordInfo = false;
                    }
                }

                // This might lead to duplicates, but if we've got multiple people
                // sharing the same Discord account with multiple connection IDs
                // then I guess it's fine to show dupes.
                hubUsers.Add(user);
            }

            await this.Clients.Caller.SendAsync("ConnectedIds", hubUsers);
        }

        public async Task HeartbeatResponse()
        {
            UserHandler.UserMappings[Context.ConnectionId].LastHeartbeatTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public override Task OnConnectedAsync()
        {
            UserHandler.ConnectedIds.Add(Context.ConnectionId);

            HubUser user = new HubUser
            {
                AspNetUsername = Context.User.Identity.Name,
                HasDiscordInfo = null
            };

            UserHandler.UserMappings.Add(Context.ConnectionId, user);
            this.Clients.All.SendAsync("ViewerConnected");

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception e)
        {
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            UserHandler.UserMappings.Remove(Context.ConnectionId);
            this.Clients.Others.SendAsync("ViewerDisconnected");
            return base.OnDisconnectedAsync(e);
        }

        public async Task AdminStreamUp()
        {
            if (this.Context.User.IsInRole("Administrator"))
            {
                this._discordBot.DiscordClient.StreamUp = true;
                await this.Clients.All.SendAsync("StreamUp");
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        public async Task AdminStreamDown()
        {
            if (this.Context.User.IsInRole("Administrator"))
            {
                this._discordBot.DiscordClient.StreamUp = false;
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        public async Task AdminForceDisconnect()
        {
            if (this.Context.User.IsInRole("Administrator"))
            {
                UserHandler.ConnectedIds.Clear();
                UserHandler.UserMappings.Clear();
                await this.Clients.All.SendAsync("ForceDisconnect");
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        public async Task AdminForceRefresh()
        {
            if (this.Context.User.IsInRole("Administrator"))
            {
                UserHandler.ConnectedIds.Clear();
                UserHandler.UserMappings.Clear();
                await this.Clients.All.SendAsync("ForceRefresh");
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        public async Task AdminSendHeartbeatRequest()
        {
            if (this.Context.User.IsInRole("Administrator"))
            {
                await this.Clients.All.SendAsync("RequestHeartbeat");
                _ = Task.Run(async () =>
                  {
                      System.Threading.Thread.Sleep(10000);
                      long heartbeatTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                      // Creates a shallow copy of the HubUser list and operates on that to avoid running into errors with
                      // foreach() on a changed IEnumerable
                      foreach (KeyValuePair<string, HubUser> userMapping in new Dictionary<string, HubUser>(UserHandler.UserMappings))
                      {
                          // If the last heartbeat time was more than 15s ago, should give some headroom with the 10s delay
                          if (heartbeatTime - userMapping.Value.LastHeartbeatTime > 15)
                          {
                              await this.Clients.Client(userMapping.Key).SendAsync("ForceDisconnect");
                              UserHandler.ConnectedIds.Remove(userMapping.Key);
                              UserHandler.UserMappings.Remove(userMapping.Key);
                              await this.Clients.Others.SendAsync("ViewerDisconnected");
                          }
                      }
                  });
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        public string RemoveMassPings(string inputString)
        {
            string outputString = inputString.Replace("@everyone", $"@{'\u200B'}everyone");
            outputString = outputString.Replace("@here", $"@{'\u200B'}here");

            return outputString;
        }
    }

    public static class UserHandler
    {
        public static HashSet<string> ConnectedIds = new HashSet<string>();
        /// <summary>
        /// The mappings for connection IDs to HubUsers.
        /// </summary>
        public static Dictionary<string, HubUser> UserMappings = new Dictionary<string, HubUser>();
    }

    public class HubUser
    {
        public string AspNetUsername { get; set; }
        public bool IsOldBrowser { get; set; }

        public bool? HasDiscordInfo { get; set; }
        public string DiscordId { get; set; }
        public string DiscordDisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public long LastHeartbeatTime { get; set; }
    }
}
