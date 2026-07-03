using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.ArpDetector
{
    public interface IArpDetector
    {
        DetectionAlert? Process(ArpPacketInfo packet);
    }
}
