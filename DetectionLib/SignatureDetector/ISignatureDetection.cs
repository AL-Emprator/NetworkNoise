using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.SignatureDetector
{
    public interface ISignatureDetection
    {
        DetectionAlert? Process(TcpPacketInfo packet);
    }
}
