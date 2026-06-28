using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class HttpPacketInfo : PacketInfo
    {
        /// <summary>HTTP method, for example GET, POST, PUT, DELETE.</summary>
        public string? Method { get; set; }

        /// <summary>Requested URL or path.</summary>
        public string? Url { get; set; }

        /// <summary>HTTP Host header.</summary>
        public string? Host { get; set; }

        /// <summary>HTTP User-Agent header.</summary>
        public string? UserAgent { get; set; }

        /// <summary>Raw HTTP payload as text.</summary>
        public string RawHttpText { get; set; } = string.Empty;

        /// <summary>True when this packet looks like an HTTP request.</summary>
        public bool IsRequest =>
            Method == "GET" ||
            Method == "POST" ||
            Method == "PUT" ||
            Method == "DELETE" ||
            Method == "HEAD" ||
            Method == "OPTIONS" ||
            Method == "PATCH";

        /// <summary>True when this packet looks like an HTTP response.</summary>
        public bool IsResponse =>
            RawHttpText.StartsWith("HTTP/");

        public string PayloadAsText =>
            Payload.Length == 0
                ? string.Empty
                : Encoding.UTF8.GetString(Payload);

        public override string ToString() =>
            $"[HTTP] {SourceIp}:{SourcePort} → {DestinationIp}:{DestinationPort} " +
            $"Method={Method} Url={Url} Host={Host} Len={Length}B @ {Timestamp:HH:mm:ss.fff}";
    }


}
