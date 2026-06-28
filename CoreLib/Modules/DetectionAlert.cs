using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class DetectionAlert
    {
        public string Severity { get; init; } = "LOW";

        public string Title { get; init; } = "";

        public string Message { get; init; } = "";

        public string SourceIp { get; init; } = "";

        public string DestinationIp { get; init; } = "";

        public int PortsScanned { get; init; }

        public double PortsPerSecond { get; init; }

        public string ScanType { get; init; } = "";

        public string MitreTechnique { get; init; } = "";

        public DateTime FirstSeen { get; init; }

        public DateTime LastSeen { get; init; }

        public DateTime Timestamp { get; init; } = DateTime.Now;
    }
}
