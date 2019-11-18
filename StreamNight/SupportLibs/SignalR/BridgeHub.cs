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
        /// <summary>
        /// Sends a message from the web chat to Discord and notifies other clients to stop the typing indicator.
        /// </summary>
        /// <param name="message">The SendMessage object representing the current operation.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task SendMessage(SendMessage message)
        {
            // Discord bridge isn't ready, so just reject the message instead of attempting to send it
            if (!_discordBot.DiscordClient.Ready)
            {
                await this.Clients.Caller.SendAsync("BridgeDown");
            }

            // Sanitise the message contents
            NewMessage newMessage = new NewMessage(RemoveMassPings(message.Content));
            // Get the internal user by the connection ID mapping
            HubUser hubUser = UserHandler.UserMappings[this.Context.ConnectionId];

            BridgeMessage bridgeMessage = new BridgeMessage();
            // Parse the logged in user's name into a Discord ID.
            // Maybe add a check here if it fails? Users should only be able to create accounts with names from their Discord IDs
            // but unclear what happens if db is corrupted etc.
            ulong.TryParse(this.Context.User.Identity.Name, out ulong userId);
            bridgeMessage.UserId = userId;

            bridgeMessage.SignalRMessage = newMessage;

            // Send the message to the Discord client
            await _discordBot.DiscordClient.IngestSignalR(bridgeMessage);
            // Tell other clients to stop the typing indicator
            await this.Clients.Others.SendAsync("StopTypingForClient", hubUser);
        }

        // No need to authorize it here because it already checks for the Manage Messages
        // permission in the Discord side.
        /// <summary>
        /// Attempts to delete the requested message from the Discord channel.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task DeleteMessage(DeleteRequest message)
        {
            // Create a new DeleteMessage object using the ID from the deletion request
            DeleteMessage deleteMessage = new DeleteMessage(message.Id);

            BridgeMessage bridgeMessage = new BridgeMessage();
            ulong.TryParse(this.Context.User.Identity.Name, out ulong userId);
            bridgeMessage.UserId = userId;

            bridgeMessage.SignalRMessage = deleteMessage;

            await _discordBot.DiscordClient.IngestSignalR(bridgeMessage);
        }

        /// <summary>
        /// Sets the old browser flag on the client in the dictionary.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task NotifyOldBrowser()
        {
            await Task.Run(() => { UserHandler.UserMappings[Context.ConnectionId].IsOldBrowser = true; });
            return;
        }

        /// <summary>
        /// Sends the client the history from the HistoryStore.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task GetHistory()
        {
            object messageHistory = new
            {
                HistoryContent = _discordBot.DiscordClient.HistoryStore.GetMessagesAsNewMessage()
            };

            // Send history to client
            await this.Clients.Caller.SendAsync("MessageHistory", messageHistory);
        }

        /// <summary>
        /// Sends other clients a typing indicator.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task ClientTyping()
        {
            HubUser hubUser = UserHandler.UserMappings[this.Context.ConnectionId];
            TypingMessage typingMessage = new TypingMessage
            {
                // Timestamp is in milliseconds to make JavaScript side easier.
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                UserId = hubUser.DiscordId
            };

            await this.Clients.Others.SendAsync("ClientTyping", typingMessage);
        }

        /// <summary>
        /// Sends the client the list of users known to be connected.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
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

        /// <summary>
        /// Processes a heartbeat response from the client.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task HeartbeatResponse()
        {
            UserHandler.UserMappings[Context.ConnectionId].LastHeartbeatTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// The method used to override the default connection handler to add user state tracking.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
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

        /// <summary>
        /// The method used to override the default connection handler to add user state tracking.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public override Task OnDisconnectedAsync(Exception e)
        {
            UserHandler.ConnectedIds.Remove(Context.ConnectionId);
            UserHandler.UserMappings.Remove(Context.ConnectionId);
            // Notify other clients of user disconnection.
            this.Clients.Others.SendAsync("ViewerDisconnected");
            return base.OnDisconnectedAsync(e);
        }

        /// <summary>
        /// Authenticates user and sends stream up notification to connected clients.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AdminStreamUp()
        {
            if (this.Context.User.IsInRole("StreamController") || this.Context.User.IsInRole("Administrator"))
            {
                this._discordBot.DiscordClient.StreamUp = true;
                await this.Clients.All.SendAsync("StreamUp");
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        /// <summary>
        /// Authenticates user and sends stream down notification to connected clients.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AdminStreamDown()
        {
            if (this.Context.User.IsInRole("StreamController") || this.Context.User.IsInRole("Administrator"))
            {
                this._discordBot.DiscordClient.StreamUp = false;
                await this.Clients.All.SendAsync("StreamDown");
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        /// <summary>
        /// Authenticates user, then sends disconnection request to all clients and clears current state tracker.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AdminForceDisconnect()
        {
            if (this.Context.User.IsInRole("StreamController") || this.Context.User.IsInRole("Administrator"))
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

        /// <summary>
        /// Authenticates user, then sends refresh request to all clients and clears current state tracker.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AdminForceRefresh()
        {
            if (this.Context.User.IsInRole("StreamController") || this.Context.User.IsInRole("Administrator"))
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

        /// <summary>
        /// Authenticates user, then sends heartbeat request to all clients and queues heartbeat checker on a background thread.
        /// </summary>
        /// <returns>A Task representing the state of the request.</returns>
        public async Task AdminSendHeartbeatRequest()
        {
            if (this.Context.User.IsInRole("StreamController") || this.Context.User.IsInRole("Administrator"))
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
                              try
                              {
                                  await this.Clients.Client(userMapping.Key).SendAsync("ForceDisconnect");
                              }
                              finally
                              {
                                  UserHandler.ConnectedIds.Remove(userMapping.Key);
                                  UserHandler.UserMappings.Remove(userMapping.Key);
                                  await this.Clients.Others.SendAsync("ViewerDisconnected");
                              }
                          }
                      }
                  });
            }
            else
            {
                await this.Clients.Caller.SendAsync("Unauthorised");
            }
        }

        /// <summary>
        /// Sanitises @here and @everyone by inserting Unicode zero-width space.
        /// </summary>
        /// <param name="inputString">The message string to sanitise.</param>
        /// <returns>The sanitised message string.</returns>
        public string RemoveMassPings(string inputString)
        {
            string outputString = inputString.Replace("@everyone", $"@{'\u200B'}everyone");
            outputString = outputString.Replace("@here", $"@{'\u200B'}here");

            return outputString;
        }
    }

    public static class UserHandler
    {
        /// <summary>
        /// A HashSet of connected IDs.
        /// </summary>
        public static HashSet<string> ConnectedIds = new HashSet<string>();
        /// <summary>
        /// The mappings for connection IDs to HubUsers.
        /// </summary>
        public static Dictionary<string, HubUser> UserMappings = new Dictionary<string, HubUser>();
    }

    public class HubUser
    {
        /// <summary>
        /// ASP.NET username of the connected user. Should match the DiscordId.
        /// </summary>
        public string AspNetUsername { get; set; }
        /// <summary>
        /// User is using IE or EdgeHTML Edge.
        /// </summary>
        public bool IsOldBrowser { get; set; }

        /// <summary>
        /// Nullable boolean representing whether Discord fields are populated.
        /// </summary>
        public bool? HasDiscordInfo { get; set; }
        /// <summary>
        /// Discord ID of user. Should be safe to cast as ulong.
        /// </summary>
        public string DiscordId { get; set; }
        /// <summary>
        /// Discord display name of user. Should be nickname if present.
        /// </summary>
        public string DiscordDisplayName { get; set; }
        /// <summary>
        /// Discord CDN avatar URL.
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// Last heartbeat time. Only populated if AdminSendHeartbeatRequest() has been run at least once.
        /// </summary>
        public long LastHeartbeatTime { get; set; }
    }
}
