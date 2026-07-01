using CoreLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DetectionLib.HttpCredentialDetection
{
    public sealed class HttpCredentialDetector : Ihttpdetector
    {

        private static readonly string[] Keywords =
 {
            "password=",
            "passwd=",
            "pwd=",
            "username=",
            "user=",
            "token=",
            "access_token=",
            "apikey=",
            "api_key="
        };

        public DetectionAlert? Process(HttpPacketInfo packet)
        {

            if (!packet.IsRequest)
                return null;


            if (!string.Equals(packet.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return null;



            var payload = packet.PayloadAsText;
            Console.WriteLine(payload);

            if (string.IsNullOrWhiteSpace(payload))
                return null;


            var username = Extract(payload, @"(?:username|user|login)=([^&\s]+)");

            var password = Extract(payload, @"(?:password|passwd|pwd)=([^&\s]+)");

            var token = Extract(payload, @"(?:token|access_token|apikey|api_key)=([^&\s]+)");

            if (username is null && password is null && token is null)
            {
                return null;
            }

            var findings = new List<string>();


            if (username is not null)
                findings.Add($"username='{username}'");

            if (password is not null)
                findings.Add($"password='{password}'");

            if (token is not null)
                findings.Add($"token='{token}'");


            foreach (var keyword in Keywords)
            {
                if (payload.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return new DetectionAlert
                    {
                        Severity = "HIGH",
                        Timestamp = packet.Timestamp,
                        Message =
                            $"Plaintext credentials — HTTP POST to {packet.DestinationIp}:{packet.DestinationPort} contains {string.Join(", ", findings)} field"
                    };
                }
            }





            return null;
        }


        private static string? Extract(string text, string pattern)
        {
            var match = Regex.Match(
                text,
                pattern,
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            return Uri.UnescapeDataString(
                match.Groups[1].Value);
        }



    }
}
