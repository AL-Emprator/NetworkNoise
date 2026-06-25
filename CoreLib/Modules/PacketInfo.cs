using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public class PacketInfo
    {

        /// <summary>Timestamp of packet capture (UTC).</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Source IP address (IPv4 or IPv6 string).</summary>
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Destination IP address (IPv4 or IPv6 string).</summary>
        public string DestinationIp { get; set; } = string.Empty;

        /// <summary>Source port number (0 if not applicable).</summary>
        public int SourcePort { get; set; }

        /// <summary>Destination port number (0 if not applicable).</summary>
        public int DestinationPort { get; set; }

        /// <summary>Human-readable protocol name (e.g. "TCP", "DNS", "HTTP").</summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>Total captured packet length in bytes.</summary>
        public int Length { get; set; }

        /// <summary>Raw application-layer payload bytes. Never null; empty when absent.</summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();

    }
}
