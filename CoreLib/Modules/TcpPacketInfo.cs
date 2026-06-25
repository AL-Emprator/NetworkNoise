using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Parsed representation of a single TCP segment.
/// Extends <see cref="PacketInfo"/> with TCP-specific header fields.
/// </summary>
/// 

namespace CoreLib.Modules
{
    public sealed class TcpPacketInfo : PacketInfo
    {

        /// <summary>
        /// Bitmask of TCP control flags present in this segment.
        /// Use the convenience properties below for individual flag checks.
        /// </summary>
        /// 

        public TcpFlags Flags { get; set; }

        /// <summary>Sender's sequence number.</summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Next sequence number the sender expects from the peer.
        /// Meaningful only when <see cref="IsAck"/> is <c>true</c>.
        /// </summary>
        public uint AcknowledgmentNumber { get; set; }

        /// <summary>TCP receive window size (bytes).</summary>
        public ushort WindowSize { get; set; }

        // ── Convenience flag accessors ───────────────────────────────────────────

        public bool IsSyn => Flags.HasFlag(TcpFlags.SYN);
        public bool IsAck => Flags.HasFlag(TcpFlags.ACK);
        public bool IsFin => Flags.HasFlag(TcpFlags.FIN);
        public bool IsRst => Flags.HasFlag(TcpFlags.RST);
        public bool IsPsh => Flags.HasFlag(TcpFlags.PSH);
        public bool IsUrg => Flags.HasFlag(TcpFlags.URG);

        /// <summary>True for the initial SYN of a three-way handshake (SYN set, ACK not set).</summary>
        public bool IsHandshakeInitiation => IsSyn && !IsAck;

        /// <summary>True for the server's SYN-ACK response.</summary>
        public bool IsHandshakeResponse => IsSyn && IsAck;

        /// <summary>Payload as UTF-8 text. Useful for HTTP/plaintext credential scanning.</summary>
        public string PayloadAsText =>
            Payload.Length == 0
                ? string.Empty
                : System.Text.Encoding.UTF8.GetString(Payload);

        public override string ToString() =>
            $"[TCP] {SourceIp}:{SourcePort} → {DestinationIp}:{DestinationPort} " +
            $"Flags={Flags} Seq={SequenceNumber} Ack={AcknowledgmentNumber} " +
            $"Win={WindowSize} Len={Length}B @ {Timestamp:HH:mm:ss.fff}";
    }
}

