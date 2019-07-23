using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.Areas.Identity.Data
{
    public class StreamNightRole : IdentityRole
    {
        public ICollection<StreamNightUserRole> UserRoles { get; set; }

        public StreamNightRole() : base()
        {
        } 

        public StreamNightRole(string roleName)
        {
            Name = roleName;
        }
    }
}
