using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class ArpPacketInfo
    {
        public DateTime Timestamp { get; set; }

        public string SenderIp { get; set; } = "";
        public string SenderMac { get; set; } = "";

        public string TargetIp { get; set; } = "";
        public string TargetMac { get; set; } = "";

        public string Operation { get; set; } = "";
    }
}
