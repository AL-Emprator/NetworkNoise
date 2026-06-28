using CoreLib.Modules;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureLib.Parsers.DNS
{
    public sealed class DnsParser : IDnsParser
    {
        public DnsPacketInfo? Parse(RawCapture rawCapture)
        {
            ArgumentNullException.ThrowIfNull(rawCapture);

            Packet frame;

            try
            {
                frame = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DnsParser] Parse error: {ex.Message}");
                return null;
            }

            var ipPacket = frame.Extract<IPPacket>();
            if (ipPacket is null)
                return null;

            var udpPacket = frame.Extract<UdpPacket>();
            if (udpPacket is null)
                return null;

            if (udpPacket.SourcePort != 53 && udpPacket.DestinationPort != 53)
                return null;

            var payload = udpPacket.PayloadData;

            if (payload is null || payload.Length < 12)
                return null;

            return BuildInfo(rawCapture, ipPacket, udpPacket, payload);

        }

        private static DnsPacketInfo? BuildInfo(
          RawCapture raw,
          IPPacket ip,
          UdpPacket udp,
          byte[] payload)
        {
            ushort transactionId = ReadUInt16(payload, 0);
            ushort flags = ReadUInt16(payload, 2);

            bool isResponse = (flags & 0x8000) != 0;

            var queryName = ReadDomainName(payload, 12, out int offsetAfterName);

            if (string.IsNullOrWhiteSpace(queryName))
                return null;

            if (payload.Length < offsetAfterName + 4)
                return null;

            ushort qtype = ReadUInt16(payload, offsetAfterName);

            return new DnsPacketInfo
            {
                Timestamp = raw.Timeval.Date,
                SourceIp = ip.SourceAddress.ToString(),
                DestinationIp = ip.DestinationAddress.ToString(),
                SourcePort = udp.SourcePort,
                DestinationPort = udp.DestinationPort,
                Protocol = "DNS",
                Length = raw.Data.Length,
                Payload = payload,

                TransactionId = transactionId,
                QueryName = queryName,
                QueryType = MapQueryType(qtype),
                IsResponse = isResponse
            };
        }


        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }


        private static string ReadDomainName(byte[] data, int offset, out int nextOffset)
        {
            var labels = new List<string>();
            nextOffset = offset;

            while (nextOffset < data.Length)
            {
                int length = data[nextOffset];

                if (length == 0)
                {
                    nextOffset++;
                    break;
                }

                if ((length & 0xC0) == 0xC0)
                {
                    nextOffset += 2;
                    break;
                }

                nextOffset++;

                if (nextOffset + length > data.Length)
                    return string.Empty;

                var label = System.Text.Encoding.ASCII.GetString(data, nextOffset, length);
                labels.Add(label);

                nextOffset += length;
            }

            return string.Join(".", labels);
        }


        private static string MapQueryType(ushort type)
        {
            return type switch
            {
                1 => "A",
                2 => "NS",
                5 => "CNAME",
                6 => "SOA",
                15 => "MX",
                16 => "TXT",
                28 => "AAAA",
                33 => "SRV",
                65 => "HTTPS",
                _ => $"TYPE{type}"
            };
        }

    }
}
