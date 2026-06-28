using CoreLib.Modules;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureLib.Parsers.HTTP
{
    public sealed class HttpParser : IHttpParser
    {
        public HttpPacketInfo? Parse(RawCapture rawCapture)
        {
            ArgumentNullException.ThrowIfNull(rawCapture);

            Packet frame;
            try
            {
                frame = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HttpParser] Parse error: {ex.Message}");
                return null;
            }

            var ipPacket = frame.Extract<IPPacket>();
            if (ipPacket is null)
                return null;

            var tcpPacket = frame.Extract<TcpPacket>();
            if (tcpPacket is null)
                return null;


            var payload = tcpPacket.PayloadData;

            if (payload is null || payload.Length == 0)
                return null;

            var payloadText = Encoding.UTF8.GetString(payload);

            if (!IsHttp(payloadText, tcpPacket))
                return null;

            return BuildInfo(rawCapture, ipPacket, tcpPacket, payloadText);

        }


        private static HttpPacketInfo BuildInfo(
            RawCapture raw,
            IPPacket ip,
            TcpPacket tcp,
            string payloadText)
        {
            var firstLine = GetFirstLine(payloadText);

            return new HttpPacketInfo
            {
                Timestamp = raw.Timeval.Date,
                SourceIp = ip.SourceAddress.ToString(),
                DestinationIp = ip.DestinationAddress.ToString(),
                SourcePort = tcp.SourcePort,
                DestinationPort = tcp.DestinationPort,
                Protocol = "HTTP",
                Length = raw.Data.Length,
                Payload = tcp.PayloadData ?? Array.Empty<byte>(),

                Method = ExtractMethod(firstLine),
                Url = ExtractUrl(firstLine),
                Host = ExtractHeader(payloadText, "Host"),
                UserAgent = ExtractHeader(payloadText, "User-Agent"),

                RawHttpText = payloadText,

            };
        }


        private static bool IsHttp(string payloadText, TcpPacket tcp)
        {
            if (tcp.SourcePort == 80 || tcp.DestinationPort == 80)
                return true;

            return payloadText.StartsWith("GET ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("POST ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("PUT ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("HEAD ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("OPTIONS ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("PATCH ", StringComparison.OrdinalIgnoreCase)
                || payloadText.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase);
        }


        private static string GetFirstLine(string text)
        {
            var index = text.IndexOf("\r\n", StringComparison.Ordinal);

            return index >= 0
                ? text[..index]
                : text;
        }



        private static string? ExtractMethod(string firstLine)
        {
            var parts = firstLine.Split(' ');

            return parts.Length >= 1
                ? parts[0]
                : null;
        }

        private static string? ExtractUrl(string firstLine)
        {
            var parts = firstLine.Split(' ');

            return parts.Length >= 2
                ? parts[1]
                : null;
        }


        private static string? ExtractHeader(string httpText, string headerName)
        {
            var lines = httpText.Split("\r\n");

            foreach (var line in lines)
            {
                if (line.StartsWith(headerName + ":", StringComparison.OrdinalIgnoreCase))
                {
                    return line[(headerName.Length + 1)..].Trim();
                }
            }

            return null;
        }


    }
}
