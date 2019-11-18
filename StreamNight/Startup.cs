using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamNight.Areas.Account.Data;
using StreamNight.Models;
using StreamNight.SupportLibs.Discord;
using StreamNight.SupportLibs.History;
using StreamNight.SupportLibs.SignalR;
using StreamNight.SupportLibs.Status;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreamNight
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // ASP.NET Identity
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Account", "/Manage");
                options.Conventions.AuthorizeAreaPage("Account", "/Logout");
            });

            services.AddAuthentication()
                .AddOAuth("Discord", "Discord", options =>
                {
                    options.AuthorizationEndpoint = "https://discordapp.com/api/oauth2/authorize";
                    options.TokenEndpoint = "https://discordapp.com/api/oauth2/token";
                    options.UserInformationEndpoint = "https://discordapp.com/api/users/@me";

                    options.ClientId = Configuration["Discord:AppId"];
                    options.ClientSecret = Configuration["Discord:AppSecret"];
                    options.Scope.Add("identify");
                    options.CallbackPath = Configuration["Discord:Callback"];

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.UInteger64);
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username", ClaimValueTypes.String);
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
                    options.ClaimActions.MapJsonKey("urn:discord:discriminator", "discriminator", ClaimValueTypes.UInteger32);
                    options.ClaimActions.MapJsonKey("urn:discord:avatar", "avatar", ClaimValueTypes.String);
                    options.ClaimActions.MapJsonKey("urn:discord:verified", "verified", ClaimValueTypes.Boolean);

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            using (HttpResponseMessage response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    response.EnsureSuccessStatusCode();
                                }

                                using var userPayload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                                context.RunClaimActions(userPayload.RootElement);
                            }
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole",
                     policy => policy.RequireRole("Administrator"));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Account/Login";
                options.LogoutPath = $"/Account/Logout";
                options.AccessDeniedPath = $"/Account/AccessDenied";
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .DisallowCredentials();
                    });
            });

            services.AddSignalR(o =>
            {
                o.KeepAliveInterval = TimeSpan.FromSeconds(3);
                o.ClientTimeoutInterval = TimeSpan.FromSeconds(15);
            });

            services.AddSingleton(new DiscordBot());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services, StreamNightAccountDbContext chatContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();

            // HTTPS is handled upstream by Caddy.
            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();

                endpoints.MapHub<BridgeHub>("/bridgehub");
                endpoints.MapHub<StatusHub>("/admin/statushub");
                endpoints.MapHub<TwitchHub>("/admin/twitchhub");
            });

            CreateUserRoles(services, chatContext).Wait();
        }

        private async Task CreateUserRoles(IServiceProvider serviceProvider, StreamNightAccountDbContext dbContext)
        {
            dbContext.Database.Migrate();

            var RoleManager = serviceProvider.GetRequiredService<RoleManager<StreamNightRole>>();

            //Adding Admin Role
            bool adminRoleCheck = await RoleManager.RoleExistsAsync("Administrator");
            if (!adminRoleCheck)
            {
                //create the roles and seed them to the database
                _ = await RoleManager.CreateAsync(new StreamNightRole("Administrator"));
            }

            //Adding Moderator Role
            bool streamRoleCheck = await RoleManager.RoleExistsAsync("StreamController");
            if (!streamRoleCheck)
            {
                //create the roles and seed them to the database
                _ = await RoleManager.CreateAsync(new StreamNightRole("StreamController"));
            }
        }
    }

    public class DiscordBot
    {
        public Client DiscordClient { get; private set; }
        private MemoryHistory memoryHistory = new MemoryHistory();

        public DiscordBot()
        {
            StartDiscordBot();
        }

        private void StartDiscordBot()
        {
            this.DiscordClient = new Client(memoryHistory);
            Task.Run(() =>
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                this.DiscordClient.RunBotAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            });
        }
    }
}
