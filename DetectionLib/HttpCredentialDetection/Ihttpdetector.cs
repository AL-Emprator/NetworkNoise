using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.HttpCredentialDetection
{
    public interface Ihttpdetector
    {

        DetectionAlert? Process(HttpPacketInfo packet);
    }
}
