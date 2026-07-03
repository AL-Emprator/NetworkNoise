using CoreLib.Modules;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureLib.Parsers.ARP
{
    public sealed class ArpParser
    {
        public ArpPacketInfo? Parse(RawCapture rawCapture)
        {
            Packet frame;

            try
            {
                frame = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
            }
            catch
            {
                return null;
            }

            var arp = frame.Extract<ArpPacket>();
            if (arp is null)
                return null;

            return new ArpPacketInfo
            {
                Timestamp = rawCapture.Timeval.Date,

                SenderIp = arp.SenderProtocolAddress.ToString(),
                SenderMac = arp.SenderHardwareAddress.ToString(),

                TargetIp = arp.TargetProtocolAddress.ToString(),
                TargetMac = arp.TargetHardwareAddress.ToString(),

                Operation = arp.Operation.ToString()
            };
        }
    }
}
