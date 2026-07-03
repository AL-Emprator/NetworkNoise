using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.ArpDetector
{
    public sealed class ArpSpoofDetector : IArpDetector
    {
        private readonly Dictionary<string, string> _ipToMac = new();

        private readonly Dictionary<string, DateTime> _lastAlert = new();

        private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(30);


        public DetectionAlert? Process(ArpPacketInfo packet)
        {
            if (string.IsNullOrWhiteSpace(packet.SenderIp) ||
                string.IsNullOrWhiteSpace(packet.SenderMac))
                return null;


            var ip = packet.SenderIp;
            var mac = NormalizeMac(packet.SenderMac);

            if (!_ipToMac.TryGetValue(ip, out var knownMac))
            {
                _ipToMac[ip] = mac;
                return null;
            
            }


            if (knownMac == mac)
                return null;

            var now = packet.Timestamp;

            if (_lastAlert.TryGetValue(ip, out var last) &&
                now - last < _cooldown)
            {
                return null;
            }

            _lastAlert[ip] = now;


            return new DetectionAlert
            {
                Severity = "HIGH",
                Title = "ARP Spoofing Suspected",
                SourceIp = packet.SenderIp,
                DestinationIp = packet.TargetIp,
                Timestamp = packet.Timestamp,
                MitreTechnique = "T1557.002 ARP Cache Poisoning",

                Message =
                    $"ARP Spoofing Suspected | IP {ip} changed MAC " +
                    $"from {knownMac} to {mac} | Target {packet.TargetIp}"
            };
        }


        private static string NormalizeMac(string mac)
        {
            return mac
                .Replace("-", ":")
                .ToUpperInvariant();
        }

    }
}
