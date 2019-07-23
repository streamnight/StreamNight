using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Models
{
    public class BridgeMessage
    {
        public ISignalRMessage SignalRMessage { get; set; }
        public ulong UserId { get; set; }
    }
}
