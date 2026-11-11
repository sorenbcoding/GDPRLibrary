using SorenB.Rules.Privacy.Git;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    /// <summary>Temporary git repos (a .git folder plus .gitignore files) on disk.</summary>
    public sealed class GitIgnoreTests : IDisposable
    {
        private readonly string _repo = Path.Combine(Path.GetTempPath(), "sb-gitignore-" + Guid.NewGuid().ToString("N"));

        public GitIgnoreTests() => Directory.CreateDirectory(Path.Combine(_repo, ".git"));

        public void Dispose() => Directory.Delete(_repo, recursive: true);

        private void WriteFile(string relativePath, params string[] lines)
        {
            var path = Path.Combine(_repo, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private bool IsIgnoredDir(string relativePath)
            => GitIgnore.IsIgnored(_repo, Path.Combine(_repo, relativePath), isDirectory: true);

        [Theory]
        [InlineData(".screenshots")]
        [InlineData(".screenshots/")]
        [InlineData("**/.screenshots")]
        [InlineData("**/.screenshots/")]
        [InlineData(".screen*")]
        [InlineData(".screensh?ts")]
        [InlineData(".screenshots/   ")]
        public void Root_pattern_matches_folder_in_subproject(string pattern)
        {
            WriteFile(".gitignore", pattern);

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Fact]
        public void Anchored_pattern_matches_only_at_its_folder()
        {
            WriteFile(".gitignore", "/.screenshots");

            Assert.True(IsIgnoredDir(".screenshots"));
            Assert.False(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Fact]
        public void Anchored_path_pattern_matches_that_path()
        {
            WriteFile(".gitignore", "Invoices/.screenshots/");

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
            Assert.False(IsIgnoredDir("Payments/.screenshots"));
        }

        [Fact]
        public void Inner_double_star_matches_any_depth()
        {
            WriteFile(".gitignore", "Projects/**/.screenshots");

            Assert.True(IsIgnoredDir("Projects/.screenshots"));
            Assert.True(IsIgnoredDir("Projects/Finance/Invoices/.screenshots"));
            Assert.False(IsIgnoredDir("Other/.screenshots"));
        }

        [Fact]
        public void Nested_gitignore_applies_to_its_own_folder()
        {
            WriteFile("Invoices/.gitignore", ".screenshots/");

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
            Assert.False(IsIgnoredDir("Payments/.screenshots"));
        }

        [Fact]
        public void Negation_in_deeper_gitignore_re_includes()
        {
            WriteFile(".gitignore", ".screenshots/");
            WriteFile("Invoices/.gitignore", "!.screenshots/");

            Assert.False(IsIgnoredDir("Invoices/.screenshots"));
            Assert.True(IsIgnoredDir("Payments/.screenshots"));
        }

        [Fact]
        public void Last_matching_line_wins()
        {
            WriteFile(".gitignore", "!.screenshots/", ".screenshots/");

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Fact]
        public void Ignored_parent_folder_ignores_the_folder()
        {
            WriteFile(".gitignore", "Sandbox/", "!Sandbox/Invoices/.screenshots/");

            // Git cannot re-include inside an excluded folder.
            Assert.True(IsIgnoredDir("Sandbox/Invoices/.screenshots"));
        }

        [Fact]
        public void Info_exclude_is_read()
        {
            WriteFile(".git/info/exclude", ".screenshots/");

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Theory]
        [InlineData("# .screenshots")]
        [InlineData("")]
        [InlineData("\\#.screenshots")]
        [InlineData("screenshots")]
        [InlineData(".screenshots.txt")]
        [InlineData("[z-a]")]
        [InlineData("  .screenshots")]
        public void Non_matching_lines_do_not_ignore(string line)
        {
            WriteFile(".gitignore", line);

            Assert.False(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Fact]
        public void Directory_only_pattern_does_not_match_a_file()
        {
            WriteFile(".gitignore", ".screenshots/");

            Assert.False(GitIgnore.IsIgnored(_repo, Path.Combine(_repo, "Invoices", ".screenshots"), isDirectory: false));
        }

        [Fact]
        public void Matching_is_case_insensitive_like_git_on_windows()
        {
            WriteFile(".gitignore", ".Screenshots/");

            Assert.True(IsIgnoredDir("Invoices/.screenshots"));
        }

        [Fact]
        public void Finds_repo_root_from_subfolder()
        {
            var project = Path.Combine(_repo, "Finance", "Invoices");
            Directory.CreateDirectory(project);

            Assert.Equal(Path.GetFullPath(_repo), GitIgnore.FindRepoRoot(project));
        }

        [Fact]
        public void Finds_repo_root_when_dot_git_is_a_file()
        {
            Directory.Delete(Path.Combine(_repo, ".git"));
            WriteFile(".git", "gitdir: ../main/.git/worktrees/feature");

            Assert.Equal(Path.GetFullPath(_repo), GitIgnore.FindRepoRoot(_repo));
        }

        [Fact]
        public void Path_outside_repo_is_not_ignored()
        {
            WriteFile(".gitignore", "*");

            Assert.False(GitIgnore.IsIgnored(_repo, Path.Combine(Path.GetTempPath(), "elsewhere", ".screenshots"), isDirectory: true));
        }
    }
}
