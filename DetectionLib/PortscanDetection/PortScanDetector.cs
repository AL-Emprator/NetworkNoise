using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.PortscanDetection
{
    public sealed class PortScanDetector : IPacketDetector
    {


        private static readonly HashSet<int> CommonPorts = BuildCommonPorts();
        private readonly TimeSpan _window = TimeSpan.FromSeconds(10);
        private readonly int _portThreshold = 20;

        private readonly Dictionary<string, List<ScanEvent>> _scanHistory = new();
        private readonly HashSet<string> _cooldowns = new();


        public DetectionAlert? Process(TcpPacketInfo packet)
        {
            if (!packet.IsSyn || packet.IsAck)
                return null;

            var sourceIp = packet.SourceIp;
            var destinationIp = packet.DestinationIp;
            var key = $"{sourceIp}->{destinationIp}";
            var now = packet.Timestamp;


            if (!_scanHistory.TryGetValue(key, out var events))
            {
                events = new List<ScanEvent>();
                _scanHistory[key] = events;
            }


            events.Add(new ScanEvent
            {
                Timestamp = now,
                DestinationIp = destinationIp,
                DestinationPort = packet.DestinationPort,
                IsCommonPort = CommonPorts.Contains(packet.DestinationPort),
                WindowSize = packet.WindowSize
            });

            events.RemoveAll(e => now - e.Timestamp > _window);

            var uniquePorts = events
                .Select(e => e.DestinationPort)
                .Distinct()
                .Count();



            Console.WriteLine(
               $"SCAN TRACK: {key} port={packet.DestinationPort} uniquePorts={uniquePorts}");


            if (_cooldowns.Contains(key))
                return null;

            if (uniquePorts < _portThreshold)
                return null;


            var duration = events.Count > 1
               ? events.Max(e => e.Timestamp) - events.Min(e => e.Timestamp)
               : TimeSpan.Zero;

            var commonCount = events.Count(e => e.IsCommonPort);
            var uncommonCount = events.Count - commonCount;

            var scanType = uncommonCount > commonCount
                ? "uncommon ports"
                : "common service ports";

            Console.WriteLine(
               $"TRIGGER: {key} ports={uniquePorts}");

            _scanHistory.Remove(key);

            _cooldowns.Add(key);

            _ = Task.Delay(10000).ContinueWith(_ =>
            {
                _cooldowns.Remove(key);
            });

            var portsPerSecond =
            duration.TotalSeconds <= 0
            ? uniquePorts
            : uniquePorts / duration.TotalSeconds;

            string severity =
                uniquePorts switch
                {
                    >= 200 => "CRITICAL",
                    >= 20 => "HIGH",
                    >= 10 => "MED",
                    _ => "LOW"
                };


            return new DetectionAlert
            {
                Severity = severity,

                Title = "SYN Port Scan",

                SourceIp = sourceIp,

                DestinationIp = destinationIp,

                PortsScanned = uniquePorts,

                PortsPerSecond = Math.Round(portsPerSecond, 1),

                ScanType = scanType,

                MitreTechnique = "T1046 Network Service Discovery",

                FirstSeen = events.Min(e => e.Timestamp),

                LastSeen = events.Max(e => e.Timestamp),

                Timestamp = now,

                Message =
                  $"[{severity}] SYN Scan | Src {sourceIp} | " +
                  $"{uniquePorts} ports | " +
                  $"{portsPerSecond:0.0} p/s | " +
                  $"{scanType} | MITRE T1046"
            };
        }


        private sealed class ScanEvent
        {
            public DateTime Timestamp { get; init; }
            public string DestinationIp { get; init; } = string.Empty;
            public int DestinationPort { get; init; }

            public bool IsCommonPort { get; init; }
            public int WindowSize { get; init; }
        }


        private static HashSet<int> BuildCommonPorts()
        {
            var ports = new HashSet<int>();

            void Add(int port) => ports.Add(port);

            void AddRange(int start, int end)
            {
                for (int i = start; i <= end; i++)
                    ports.Add(i);
            }

            Add(7);
            Add(9);
            Add(13);
            AddRange(21, 23);
            AddRange(25, 26);
            Add(37);
            Add(53);
            AddRange(79, 81);
            Add(88);
            Add(106);
            AddRange(110, 111);
            Add(113);
            Add(119);
            Add(135);
            Add(139);
            AddRange(143, 144);
            Add(179);
            Add(199);
            Add(389);
            Add(427);
            AddRange(443, 445);
            Add(465);
            AddRange(513, 515);
            AddRange(543, 544);
            Add(548);
            Add(554);
            Add(587);
            Add(631);
            Add(646);
            Add(873);
            Add(990);
            Add(993);
            Add(995);
            AddRange(1025, 1029);
            Add(1110);
            Add(1433);
            Add(1720);
            Add(1723);
            Add(1755);
            Add(1900);
            AddRange(2000, 2001);
            Add(2049);
            Add(2121);
            Add(2717);
            Add(3000);
            Add(3128);
            Add(3306);
            Add(3389);
            Add(3986);
            Add(4899);
            Add(5000);
            Add(5009);
            Add(5051);
            Add(5060);
            Add(5101);
            Add(5190);
            Add(5357);
            Add(5432);
            Add(5631);
            Add(5666);
            Add(5800);
            Add(5900);
            AddRange(6000, 6001);
            Add(6646);
            Add(7070);
            Add(8000);
            AddRange(8008, 8009);
            AddRange(8080, 8081);
            Add(8443);
            Add(8888);
            AddRange(9999, 10000);
            Add(32768);
            AddRange(49152, 49157);

            return ports;
        }
    }
}
