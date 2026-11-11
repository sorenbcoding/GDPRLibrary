using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>
    /// Approved domains (semicolon-separated). "*.dk" matches dk-hosts and any subdomain; "api.example.com" matches that host only.
    /// Internal hosts (no dot, localhost, private or loopback IPs) are always approved.
    /// </summary>
    internal sealed class DomainList
    {
        private const string UrlPattern = @"https?://[^\s""'<>\\{}|^`]+";

        private readonly List<string> _exact;
        private readonly List<string> _suffixes;

        private DomainList(List<string> exact, List<string> suffixes)
        {
            _exact = exact;
            _suffixes = suffixes;
        }

        internal static DomainList Parse(string? list)
        {
            var exact = new List<string>();
            var suffixes = new List<string>();
            foreach (var raw in (list ?? string.Empty).Split(new[] { ';', ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var entry = raw.Trim().TrimEnd('.').ToLowerInvariant();
                if (entry.StartsWith("*.", StringComparison.Ordinal) && entry.Length > 2)
                {
                    suffixes.Add(entry.Substring(2));
                }
                else if (entry.Length > 0 && entry != "*")
                {
                    exact.Add(entry);
                }
            }

            return new DomainList(exact, suffixes);
        }

        internal bool IsApproved(string host)
        {
            var h = host.Trim().TrimEnd('.').ToLowerInvariant();
            if (IsInternal(h))
            {
                return true;
            }

            return _exact.Contains(h)
                || _suffixes.Any(s => h == s || h.EndsWith("." + s, StringComparison.Ordinal));
        }

        /// <summary>The hosts of http(s) URLs in a piece of literal text. URLs that don't parse (placeholders like {0}) are skipped.</summary>
        internal static IEnumerable<string> HostsIn(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            var regex = SafeRegex.Get(UrlPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (regex is null)
            {
                yield break;
            }

            MatchCollection matches;
            try
            {
                matches = regex.Matches(text);
                _ = matches.Count;
            }
            catch (RegexMatchTimeoutException)
            {
                yield break;
            }

            foreach (Match match in matches)
            {
                if (Uri.TryCreate(match.Value.TrimEnd('.', ',', ')', ';'), UriKind.Absolute, out var uri) && uri.Host.Length > 0)
                {
                    yield return uri.IdnHost.ToLowerInvariant();
                }
            }
        }

        internal static bool IsInternal(string host)
        {
            if (host == "localhost")
            {
                return true;
            }

            var bare = host.Trim('[', ']');
            if (IPAddress.TryParse(bare, out var ip))
            {
                return IsPrivate(ip);
            }

            // A single-label name (http://intranet) only resolves on the local network.
            return !host.Contains('.');
        }

        private static bool IsPrivate(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = ip.GetAddressBytes();
                return b[0] == 10
                    || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                    || (b[0] == 192 && b[1] == 168)
                    || (b[0] == 169 && b[1] == 254);
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var b = ip.GetAddressBytes();
                return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || (b[0] & 0xFE) == 0xFC;
            }

            return false;
        }
    }
}
