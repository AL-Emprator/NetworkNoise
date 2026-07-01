using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.DnsTunnelDetection
{
    public sealed class DnsTunnelDetector : IDNSdetector
    {
        public DetectionAlert? Process(DnsPacketInfo packet)
        {

            var domain = packet.QueryName;

            if (string.IsNullOrWhiteSpace(domain))
                return null;


            // ── Rule 1: very long domain ─────────────────────

            if (domain.Length > 50)
            {
                return BuildAlert(
                    packet,
                    "Very long DNS query");
            }


            // ── Rule 2: too many subdomains ─────────────────

            int subdomains = domain.Count(c => c == '.');

            if (subdomains >= 5)
            {
                return BuildAlert(
                    packet,
                    "Too many DNS subdomains");
            }


            // ── Rule 3: high entropy/randomness ─────────────

            double entropy = CalculateEntropy(domain);

            if (entropy > 4.0)
            {
                return BuildAlert(
                    packet,
                    $"High DNS entropy ({entropy:0.00})");
            }

            return null;

        }


        private static DetectionAlert BuildAlert( DnsPacketInfo packet,   string reason)
        {
            return new DetectionAlert
            {
                Severity = "HIGH",

                Title = "DNS Tunnel Suspected",

                SourceIp = packet.SourceIp,

                DestinationIp = packet.DestinationIp,

                MitreTechnique = "T1071.004 DNS",

                Timestamp = packet.Timestamp,

                Message =
                    $"DNS Tunnel Suspected | " +
                    $"{reason} | " +
                    $"{packet.QueryName}"
            };
        }


        private static double CalculateEntropy(string input)
        {
            var groups = input
                .GroupBy(c => c)
                .Select(g => (double)g.Count() / input.Length);

            double entropy = 0;

            foreach (var p in groups)
            {
                entropy -= p * Math.Log2(p);
            }

            return entropy;
        }



    }
}
