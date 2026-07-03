using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Modules
{
    public sealed class ImportedRules
    {
        public List<string> MaliciousIps { get; set; } = new();
        public List<string> MaliciousDomains { get; set; } = new();

        public List<SignatureRule> Signatures { get; set; } = new();

    }
}
