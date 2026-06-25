using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib
{

    [Flags]
    public enum TcpFlags
    {

        None = 0,

        /// <summary>No more data from sender (connection teardown).</summary>
        FIN = 1 << 0,

        /// <summary>Synchronize sequence numbers (connection initiation).</summary>
        SYN = 1 << 1,

        /// <summary>Reset the connection.</summary>
        RST = 1 << 2,

        /// <summary>Push buffered data to the receiver application immediately.</summary>
        PSH = 1 << 3,

        /// <summary>Acknowledgment field is significant.</summary>
        ACK = 1 << 4,

        /// <summary>Urgent pointer field is significant.</summary>
        URG = 1 << 5,



    }


}
