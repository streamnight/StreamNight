using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace StreamNight.Areas.Account.Data
{
    // Add profile data for application users by adding properties to the StreamNightUser class
    public class StreamNightUser : IdentityUser
    {
        public ICollection<StreamNightUserRole> UserRoles { get; set; }
        public ulong DiscordId { get; set; }
    }
}
