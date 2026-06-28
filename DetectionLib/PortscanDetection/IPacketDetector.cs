using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.PortscanDetection
{
    public interface IPacketDetector
    {

        DetectionAlert? Process(TcpPacketInfo packet);

    }
}
