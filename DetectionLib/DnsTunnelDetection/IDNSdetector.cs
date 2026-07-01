using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.DnsTunnelDetection
{
    public interface IDNSdetector
    {
        DetectionAlert? Process(DnsPacketInfo packet);
    }
}
