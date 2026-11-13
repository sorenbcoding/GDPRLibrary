using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>Compiled, cached regexes with a match timeout. Invalid patterns return null instead of throwing.</summary>
    internal static class SafeRegex
    {
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        // One compiled Regex per pattern and options; null marks an invalid pattern.
        private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex?> Cache = new();

        internal static Regex? Get(string pattern, RegexOptions options = RegexOptions.CultureInvariant)
            => Cache.GetOrAdd((pattern, options), key => Create(key.Pattern, key.Options));

        internal static bool IsMatch(Regex regex, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                return regex.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static Regex? Create(string pattern, RegexOptions options)
        {
            try
            {
                return new Regex(pattern, options | RegexOptions.Compiled, MatchTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    /// <summary>The Danish CPR number pattern shared by the personal-data rules.</summary>
    internal static class CprPattern
    {
        /// <summary>DDMMYY, optional dash, 4 digits.</summary>
        internal const string Pattern = "(0[1-9]|[12]\\d|3[01])(0[1-9]|1[0-2])\\d{2}[-]?\\d{4}";

        internal static Regex Regex => SafeRegex.Get(Pattern)!;

        internal static bool IsMatch(string? value) => SafeRegex.IsMatch(Regex, value);
    }
}
