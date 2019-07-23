using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.Areas.Identity.Data
{
    public class StreamNightUserRole : IdentityUserRole<string>
    {
        public virtual StreamNightUser User { get; set; }
        public virtual StreamNightRole Role { get; set; }
    }
}
