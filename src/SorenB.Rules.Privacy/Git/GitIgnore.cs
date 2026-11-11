using System.Text;
using System.Text.RegularExpressions;

namespace SorenB.Rules.Privacy.Git
{
    /// <summary>
    /// Decides whether a path is ignored by git, without the git CLI or libgit2.
    /// Reads .git/info/exclude and every .gitignore from the repo root down to the path.
    /// Not read: the global core.excludesFile (per-PC, not shared with the team).
    /// </summary>
    internal static class GitIgnore
    {
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        /// <summary>The nearest folder at or above <paramref name="startDirectory"/> that contains .git (folder or file), or null.</summary>
        internal static string? FindRepoRoot(string startDirectory)
        {
            for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
            {
                var dotGit = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(dotGit) || File.Exists(dotGit))
                {
                    return dir.FullName;
                }
            }

            return null;
        }

        /// <summary>
        /// True if <paramref name="fullPath"/> (inside <paramref name="repoRoot"/>) is ignored,
        /// either itself or because one of its parent folders is ignored.
        /// </summary>
        internal static bool IsIgnored(string repoRoot, string fullPath, bool isDirectory)
        {
            var relative = Path.GetRelativePath(repoRoot, fullPath).Replace('\\', '/');
            if (relative == "." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                return false;
            }

            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var patterns = LoadPatterns(repoRoot, parts);

            // Git cannot re-include a path whose parent folder is excluded, so the first ignored level decides.
            for (var depth = 1; depth <= parts.Length; depth++)
            {
                var path = string.Join('/', parts, 0, depth);
                var isDir = depth < parts.Length || isDirectory;
                if (IsIgnoredAtLevel(patterns, path, isDir))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIgnoredAtLevel(IReadOnlyList<Pattern> patterns, string path, bool isDirectory)
        {
            var ignored = false;
            foreach (var pattern in patterns)
            {
                // Patterns from a .gitignore apply only to paths below its folder.
                if (pattern.BaseDir.Length > 0 && !path.StartsWith(pattern.BaseDir + "/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (pattern.DirectoryOnly && !isDirectory)
                {
                    continue;
                }

                var relative = pattern.BaseDir.Length > 0 ? path.Substring(pattern.BaseDir.Length + 1) : path;
                var subject = pattern.Anchored ? relative : relative.Substring(relative.LastIndexOf('/') + 1);

                try
                {
                    if (pattern.Regex.IsMatch(subject))
                    {
                        // Last match wins.
                        ignored = !pattern.Negated;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Treat an unmatchable pattern as no match.
                }
            }

            return ignored;
        }

        /// <summary>Patterns in git precedence order (lowest first): info/exclude, then .gitignore files from the root down.</summary>
        private static List<Pattern> LoadPatterns(string repoRoot, string[] parts)
        {
            var patterns = new List<Pattern>();
            AddFile(patterns, Path.Combine(repoRoot, ".git", "info", "exclude"), baseDir: string.Empty);

            // .gitignore in the root and in every parent folder of the target.
            for (var depth = 0; depth < parts.Length; depth++)
            {
                var baseDir = string.Join('/', parts, 0, depth);
                var folder = depth == 0 ? repoRoot : Path.Combine(repoRoot, Path.Combine(parts.Take(depth).ToArray()));
                AddFile(patterns, Path.Combine(folder, ".gitignore"), baseDir);
            }

            return patterns;
        }

        private static void AddFile(List<Pattern> patterns, string file, string baseDir)
        {
            if (!File.Exists(file))
            {
                return;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var line in lines)
            {
                var pattern = Parse(line, baseDir);
                if (pattern is not null)
                {
                    patterns.Add(pattern);
                }
            }
        }

        internal static Pattern? Parse(string line, string baseDir)
        {
            var text = TrimTrailingSpaces(line);
            if (text.Length == 0 || text.StartsWith('#'))
            {
                return null;
            }

            var negated = false;
            if (text.StartsWith('!'))
            {
                negated = true;
                text = text.Substring(1);
            }
            else if (text.StartsWith("\\#", StringComparison.Ordinal) || text.StartsWith("\\!", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }

            var directoryOnly = false;
            if (text.EndsWith('/'))
            {
                directoryOnly = true;
                text = text.TrimEnd('/');
            }

            if (text.Length == 0)
            {
                return null;
            }

            // A slash at the start or in the middle anchors the pattern to the .gitignore's folder.
            var anchored = text.Contains('/');
            text = text.TrimStart('/');

            try
            {
                var regex = new Regex(GlobToRegex(text), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
                return new Pattern(regex, negated, directoryOnly, anchored, baseDir);
            }
            catch (ArgumentException)
            {
                // Malformed pattern, e.g. "[z-a]": skip it.
                return null;
            }
        }

        private static string TrimTrailingSpaces(string line)
        {
            var end = line.Length;
            while (end > 0 && line[end - 1] == ' ' && !(end > 1 && line[end - 2] == '\\'))
            {
                end--;
            }

            return line.Substring(0, end).TrimStart('﻿');
        }

        private static string GlobToRegex(string glob)
        {
            var sb = new StringBuilder("^");
            for (var i = 0; i < glob.Length; i++)
            {
                var c = glob[i];
                if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
                {
                    var atStart = i == 0 || glob[i - 1] == '/';
                    var slashAfter = i + 2 < glob.Length && glob[i + 2] == '/';
                    var atEnd = i + 2 == glob.Length;
                    if (atStart && slashAfter)
                    {
                        sb.Append("(?:.*/)?"); // "**/" : any number of folders, including none
                        i += 2;
                        continue;
                    }

                    if (atStart && atEnd)
                    {
                        sb.Append(".*"); // trailing "/**" : everything inside
                        i += 1;
                        continue;
                    }

                    sb.Append("[^/]*"); // "**" elsewhere behaves like "*"
                    i += 1;
                    continue;
                }

                switch (c)
                {
                    case '*':
                        sb.Append("[^/]*");
                        break;
                    case '?':
                        sb.Append("[^/]");
                        break;
                    case '[':
                        var close = glob.IndexOf(']', i + 1);
                        if (close > i + 1)
                        {
                            var set = glob.Substring(i + 1, close - i - 1).Replace("\\", "\\\\");
                            if (set.StartsWith('!'))
                            {
                                set = "^" + set.Substring(1);
                            }

                            sb.Append('[').Append(set).Append(']');
                            i = close;
                        }
                        else
                        {
                            sb.Append("\\[");
                        }

                        break;
                    case '\\' when i + 1 < glob.Length:
                        sb.Append(Regex.Escape(glob[++i].ToString()));
                        break;
                    default:
                        sb.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            return sb.Append('$').ToString();
        }

        internal sealed record Pattern(Regex Regex, bool Negated, bool DirectoryOnly, bool Anchored, string BaseDir);
    }
}
