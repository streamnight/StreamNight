using System;
using StreamNight.Areas.Account.Data;
using StreamNight.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(StreamNight.Areas.Account.IdentityHostingStartup))]
namespace StreamNight.Areas.Account
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<StreamNightAccountDbContext>(options =>
                    options.UseSqlite(
                        context.Configuration.GetConnectionString("StreamNightContextConnection")));

                services.AddIdentity<StreamNightUser, StreamNightRole>()
                    .AddRoleManager<RoleManager<StreamNightRole>>()
                    .AddEntityFrameworkStores<StreamNightAccountDbContext>();
            });
        }
    }
}