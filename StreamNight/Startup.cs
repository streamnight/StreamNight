using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamNight.Areas.Account.Data;
using StreamNight.Models;
using StreamNight.SupportLibs.Discord;
using StreamNight.SupportLibs.History;
using StreamNight.SupportLibs.SignalR;
using DSharpPlus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNight.SupportLibs.Status;

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
            services.AddAuthentication()
            .AddDiscord(x =>
            {
                x.AppId = Configuration["Discord:AppId"];
                x.AppSecret = Configuration["Discord:AppSecret"];
                x.Scope.Add("identify");
                x.CallbackPath = Configuration["Discord:Callback"];
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole",
                     policy => policy.RequireRole("Administrator"));
            });

            // ASP.NET Identity
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2).AddRazorPagesOptions(options =>
            {
                options.AllowAreas = true;
                options.Conventions.AuthorizeAreaFolder("Account", "/Manage");
                options.Conventions.AuthorizeAreaPage("Account", "/Logout");
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider services, StreamNightAccountDbContext chatContext)
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
            app.UseAuthentication();

            // HTTPS is handled upstream by Caddy.
            // app.UseHttpsRedirection();

            app.UseSignalR(routes =>
            {
                routes.MapHub<BridgeHub>("/bridgehub");
                routes.MapHub<StatusHub>("/admin/statushub");
            });

            app.UseMvc();

            CreateUserRoles(services, chatContext).Wait();
        }

        private async Task CreateUserRoles(IServiceProvider serviceProvider, StreamNightAccountDbContext dbContext)
        {
            dbContext.Database.Migrate();

            var RoleManager = serviceProvider.GetRequiredService<RoleManager<StreamNightRole>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<StreamNightUser>>();

            IdentityResult roleResult;
            //Adding Admin Role
            bool adminRoleCheck = await RoleManager.RoleExistsAsync("Administrator");
            if (!adminRoleCheck)
            {
                //create the roles and seed them to the database
                roleResult = await RoleManager.CreateAsync(new StreamNightRole("Administrator"));
            }

            //Adding Moderator Role
            bool streamRoleCheck = await RoleManager.RoleExistsAsync("StreamController");
            if (!streamRoleCheck)
            {
                //create the roles and seed them to the database
                roleResult = await RoleManager.CreateAsync(new StreamNightRole("StreamController"));
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
