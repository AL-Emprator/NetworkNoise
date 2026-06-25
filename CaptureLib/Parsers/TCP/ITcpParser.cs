using CoreLib.Modules;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contract for parsing raw captured frames into <see cref="TcpPacketInfo"/>.
/// Keeping a thin interface here enables unit-testing without a live NIC.
/// </summary>
/// 

namespace CaptureLib.Parsers.TCP
{
    public interface ITcpParser
    {
        TcpPacketInfo? Parse(RawCapture rawCapture);
    }
}
