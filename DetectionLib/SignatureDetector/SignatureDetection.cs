using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectionLib.SignatureDetector
{
    public sealed class SignatureDetection : ISignatureDetection
    {
        private List<SignatureRule> _rules = new();

        public void LoadRules(IEnumerable<SignatureRule> rules)
        {
            _rules = rules
                .Where(r => !string.IsNullOrWhiteSpace(r.Contains))
                .ToList();
        }


        public DetectionAlert? Process(TcpPacketInfo packet)
        {
            var payload = packet.PayloadAsText;

            if (string.IsNullOrWhiteSpace(payload))
                return null;

            foreach (var rule in _rules)
            {
                if (payload.Contains(rule.Contains, StringComparison.OrdinalIgnoreCase))
                {

                    var preview =
                    payload.Length > 80
                        ? payload[..80]
                        : payload;

                    preview = preview
                        .Replace("\r", " ")
                        .Replace("\n", " ");


                    return new DetectionAlert
                    {
                        Severity = rule.Severity,
                        Title = rule.Name,
                        SourceIp = packet.SourceIp,
                        DestinationIp = packet.DestinationIp,
                        MitreTechnique = rule.Mitre,
                        Timestamp = packet.Timestamp,

                        Message =
                                    $"Signature matched | " +
                                    $"{rule.Name} | " +
                                    $"Pattern=\"{rule.Contains}\" | " +
                                    $"Payload=\"{preview}\" | " +
                                    $"{packet.SourceIp} → {packet.DestinationIp} | " +
                                    $"{rule.Mitre}"
                    };
                }
            }


            return null;
        }

    }
}
