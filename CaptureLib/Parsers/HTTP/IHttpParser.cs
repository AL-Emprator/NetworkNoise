using CoreLib.Modules;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureLib.Parsers.HTTP
{
    public interface IHttpParser
    {
        /// <summary>
        /// Attempts to decode <paramref name="rawCapture"/> as an HTTP packet.
        /// </summary>
        /// <param name="rawCapture">Raw frame as delivered by SharpPcap.</param>
        /// <returns>
        /// A populated <see cref="HttpPacketInfo"/> when the frame contains HTTP traffic;
        /// otherwise <c>null</c>.
        /// </returns>
        HttpPacketInfo? Parse(RawCapture rawCapture);

    }
}
