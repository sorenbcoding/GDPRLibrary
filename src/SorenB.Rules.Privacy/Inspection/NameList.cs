namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>
    /// A configurable list of names (semicolon-separated, case-insensitive) matched against whole words of identifiers.
    /// ElevNavn -> words elev, navn; Holdnavn -> one word holdnavn. A word matches only if it is in the list,
    /// unless the identifier, or the word joined with the word before it, is on the ignore list (Fil + Navn -> filnavn).
    /// </summary>
    internal sealed class NameList
    {
        private readonly HashSet<string> _names;
        private readonly HashSet<string> _ignored;

        private NameList(HashSet<string> names, HashSet<string> ignored)
        {
            _names = names;
            _ignored = ignored;
        }

        internal bool IsEmpty => _names.Count == 0;

        internal static NameList Parse(string? names, string? ignored = null) => new(Split(names), Split(ignored));

        /// <summary>The list entry an identifier matches, or null.</summary>
        internal string? Match(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || IsEmpty)
            {
                return null;
            }

            var whole = new string(identifier.Where(char.IsLetter).ToArray()).ToLowerInvariant();
            if (_ignored.Contains(whole))
            {
                return null;
            }

            if (_names.Contains(whole))
            {
                return whole;
            }

            var words = Expression.Words(identifier);
            for (var i = 0; i < words.Count; i++)
            {
                if (!_names.Contains(words[i]))
                {
                    continue;
                }

                if (i > 0 && _ignored.Contains(words[i - 1] + words[i]))
                {
                    continue;
                }

                return words[i];
            }

            return null;
        }

        private static HashSet<string> Split(string? list)
            => new(
                (list ?? string.Empty)
                    .Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Where(s => s.Length > 0),
                StringComparer.Ordinal);
    }
}
