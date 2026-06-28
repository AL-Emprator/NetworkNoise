using CoreLib.Modules;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureLib.Parsers.DNS
{
    public interface IDnsParser
    {
        DnsPacketInfo? Parse(RawCapture rawCapture);
    }
}
