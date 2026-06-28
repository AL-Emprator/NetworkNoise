    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class DnsPacketInfo : PacketInfo
    {
        public ushort TransactionId { get; set; }

        public string QueryName { get; set; } = string.Empty;

        public string QueryType { get; set; } = string.Empty;

        public bool IsResponse { get; set; }

        public bool IsQuery => !IsResponse;

        public string PayloadAsText =>
            Payload.Length == 0
                ? string.Empty
                : Encoding.UTF8.GetString(Payload);

        public override string ToString() =>
            $"[DNS] {SourceIp}:{SourcePort} → {DestinationIp}:{DestinationPort} " +
            $"Query={QueryName} Type={QueryType} Response={IsResponse} Len={Length}B";
    }
}
