using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs.Models
{
    public interface ISignalRMessage
    {
        string Action { get; }

        string MessageId { get; }
    }
}
