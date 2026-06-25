using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class PacketRow
    {
        public DateTime Timestamp { get; init; }
        public string Protocol { get; init; } = "";
        public string SourceIp { get; init; } = "";
        public int SourcePort { get; init; }
        public string DestinationIp { get; init; } = "";
        public int DestinationPort { get; init; }
        public string Info { get; init; } = "";
        public string Length { get; init; } = "";
        public string PayloadPreview { get; init; } = "";
        public string FullPayload { get; init; } = "";
        public bool IsSyn { get; init; }
        public bool IsHttp { get; init; }


    }
}
