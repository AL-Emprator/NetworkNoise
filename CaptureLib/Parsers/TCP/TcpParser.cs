using CoreLib;
using CoreLib.Modules;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CaptureLib.Parsers.TCP

/// <summary>
/// Parses raw SharpPcap frames into <see cref="TcpPacketInfo"/> models.
///
/// Pipeline:
///   RawCapture  →  PacketDotNet  →  IPPacket  →  TcpPacket  →  TcpPacketInfo
///
/// Non-TCP frames are silently discarded (returns null).
/// This class is stateless and thread-safe; a single instance may be shared
/// across the capture thread and any worker threads.
/// </summary>
/// 



{
    public sealed class TcpParser : ITcpParser
    {
        public TcpPacketInfo? Parse(RawCapture rawCapture)
        {
            ArgumentNullException.ThrowIfNull(rawCapture);

            // ── Layer 2 → top-level packet ────────────────────────────────────
            Packet frame;
            try
            {
                frame = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
            }
            catch (Exception ex)
            {
                // Malformed/truncated frame — swallow and skip
                System.Diagnostics.Debug.WriteLine($"[TcpParser] Parse error: {ex.Message}");
                return null;
            }

            // ── Layer 3: require an IP packet ─────────────────────────────────
            var ipPacket = frame.Extract<IPPacket>();
            if (ipPacket is null)
                return null;

            // ── Layer 4: require TCP ──────────────────────────────────────────
            var tcpPacket = frame.Extract<TcpPacket>();
            if (tcpPacket is null)
                return null;

            return BuildInfo(rawCapture, ipPacket, tcpPacket);
        }


        private static TcpPacketInfo BuildInfo(
        RawCapture raw,
        IPPacket ip,
        TcpPacket tcp)
        {

            return new TcpPacketInfo
            {
                // Base PacketInfo fields
                Timestamp = raw.Timeval.Date,
                SourceIp = ip.SourceAddress.ToString(),
                DestinationIp = ip.DestinationAddress.ToString(),
                SourcePort = tcp.SourcePort,
                DestinationPort = tcp.DestinationPort,
                Protocol = "TCP",
                Length = raw.Data.Length,
                Payload = tcp.PayloadData ?? Array.Empty<byte>(),

                // TCP-specific header fields
                Flags = ExtractFlags(tcp),
                SequenceNumber = tcp.SequenceNumber,
                AcknowledgmentNumber = tcp.AcknowledgmentNumber,
                WindowSize = tcp.WindowSize,
            };
        }


        /// <summary>
        /// Maps PacketDotNet's individual flag booleans onto our <see cref="TcpFlags"/> enum.
        /// Isolating this keeps the flag logic visible and easily testable.
        /// </summary>
        private static TcpFlags ExtractFlags(TcpPacket tcp)
        {
            var flags = TcpFlags.None;

            if (tcp.Synchronize) flags |= TcpFlags.SYN;
            if (tcp.Acknowledgment) flags |= TcpFlags.ACK;
            if (tcp.Finished) flags |= TcpFlags.FIN;
            if (tcp.Reset) flags |= TcpFlags.RST;
            if (tcp.Push) flags |= TcpFlags.PSH;
            if (tcp.Urgent) flags |= TcpFlags.URG;

            return flags;
        }


    }
}
