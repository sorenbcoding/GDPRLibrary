using System.Text;

namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>
    /// Light-weight reading of VB and C# workflow expressions: identifiers, string literals and indexer keys.
    /// Not a parser; good enough to see which names an expression uses and which literal text it contains.
    /// </summary>
    internal static class Expression
    {
        /// <summary>
        /// Names the expression refers to: identifiers outside strings, plus string literals used as indexer keys
        /// (row("ElevNavn"), dict["email"]). Plain literal text such as "Processing record" is not a name.
        /// </summary>
        internal static IReadOnlyList<string> Names(string? expression, bool csharp = false)
        {
            var names = new List<string>();
            foreach (var token in Tokenize(expression, csharp))
            {
                if (token.Kind == TokenKind.Identifier
                    || (token.Kind == TokenKind.String && token.IsIndexerKey && IsIdentifierLike(token.Text)))
                {
                    names.Add(token.Text);
                }
            }

            return names;
        }

        /// <summary>The contents of every string literal in the expression.</summary>
        internal static IReadOnlyList<string> StringLiterals(string? expression, bool csharp = false)
            => Tokenize(expression, csharp).Where(t => t.Kind == TokenKind.String).Select(t => t.Text).ToList();

        /// <summary>
        /// The text of a literal string value, or null if the value is an expression.
        /// Accepts a quoted literal ("abc", as the analyzer model reports Literal&lt;string&gt;) and,
        /// when <paramref name="isLiteral"/> is set, unquoted text.
        /// </summary>
        internal static string? LiteralValue(string? expression, bool isLiteral, bool csharp = false)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return null;
            }

            var trimmed = expression.Trim();
            var text = StripBrackets(trimmed);
            var tokens = Tokenize(text, csharp).ToList();
            if (tokens.Count == 1 && tokens[0].Kind == TokenKind.String && tokens[0].Start == 0 && tokens[0].End == text.Length)
            {
                return tokens[0].Text;
            }

            // [expr] is always an expression; {x:Null} is no value.
            return isLiteral && !trimmed.StartsWith('[') && !text.Equals("{x:Null}", StringComparison.Ordinal) ? text : null;
        }

        private static bool IsIdentifierLike(string text)
            => text.Length > 0 && (char.IsLetter(text[0]) || text[0] == '_') && text.All(c => char.IsLetterOrDigit(c) || c == '_');

        /// <summary>Splits an identifier into lower-case words on '_', digits and camelCase: dtr_NyAnsøger -> dtr, ny, ansøger.</summary>
        internal static IReadOnlyList<string> Words(string identifier)
        {
            var words = new List<string>();
            var current = new StringBuilder();

            void Flush()
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }
            }

            for (var i = 0; i < identifier.Length; i++)
            {
                var c = identifier[i];
                if (!char.IsLetter(c))
                {
                    Flush();
                    continue;
                }

                if (char.IsUpper(c) && current.Length > 0)
                {
                    var previousLower = char.IsLower(identifier[i - 1]);
                    var nextLower = i + 1 < identifier.Length && char.IsLower(identifier[i + 1]);
                    var previousUpper = char.IsUpper(identifier[i - 1]);
                    if (previousLower || (previousUpper && nextLower))
                    {
                        Flush();
                    }
                }

                current.Append(c);
            }

            Flush();
            return words;
        }

        private static string StripBrackets(string text)
            => text.Length >= 2 && text[0] == '[' && text[^1] == ']' ? text.Substring(1, text.Length - 2).Trim() : text;

        private enum TokenKind
        {
            Identifier,
            String,
        }

        private sealed record Token(TokenKind Kind, string Text, int Start, int End, bool IsIndexerKey);

        private static IEnumerable<Token> Tokenize(string? expression, bool csharp)
        {
            if (string.IsNullOrEmpty(expression))
            {
                yield break;
            }

            var text = expression;
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];

                if (c == '"')
                {
                    var start = i;
                    var value = new StringBuilder();
                    i++;
                    while (i < text.Length)
                    {
                        if (!csharp && text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"')
                        {
                            value.Append('"'); // VB escaped quote ""
                            i += 2;
                        }
                        else if (csharp && text[i] == '\\' && i + 1 < text.Length)
                        {
                            value.Append(text[i + 1]); // C# escape \" \\ …; VB has none, so "C:\" closes there
                            i += 2;
                        }
                        else if (text[i] == '"')
                        {
                            i++;
                            break;
                        }
                        else
                        {
                            value.Append(text[i]);
                            i++;
                        }
                    }

                    yield return new Token(TokenKind.String, value.ToString(), start, i, IsIndexerKey(text, start, i));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var start = i;
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                    }

                    yield return new Token(TokenKind.Identifier, text.Substring(start, i - start), start, i, false);
                    continue;
                }

                i++;
            }
        }

        /// <summary>A literal directly between ( ) or [ ], e.g. row("ElevNavn") or dict["email"].</summary>
        private static bool IsIndexerKey(string text, int start, int end)
        {
            var before = start - 1;
            while (before >= 0 && char.IsWhiteSpace(text[before]))
            {
                before--;
            }

            var after = end;
            while (after < text.Length && char.IsWhiteSpace(text[after]))
            {
                after++;
            }

            return before >= 0 && after < text.Length
                && ((text[before] == '(' && text[after] == ')') || (text[before] == '[' && text[after] == ']'))
                && before > 0 && (char.IsLetterOrDigit(text[before - 1]) || text[before - 1] == '_' || text[before - 1] == ')' || text[before - 1] == ']');
        }
    }
}
