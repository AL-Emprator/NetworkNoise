    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class SignatureRule
    {
        public string Name { get; set; } = "";
        public string Severity { get; set; } = "MED";
        public string Protocol { get; set; } = "HTTP";
        public string Contains { get; set; } = "";
        public string Mitre { get; set; } = "";
    }
}
