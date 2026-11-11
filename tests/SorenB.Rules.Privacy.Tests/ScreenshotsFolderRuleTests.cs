using System.Diagnostics;
using NSubstitute;
using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    public sealed class ScreenshotsFolderRuleTests : IDisposable
    {
        // <temp>/<root>/Invoices is the project; <root> becomes a git repo when a test calls MakeRepo().
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sb-sec-002-" + Guid.NewGuid().ToString("N"));
        private readonly string _projectDir;

        public ScreenshotsFolderRuleTests()
        {
            _projectDir = Path.Combine(_root, "Invoices");
            Directory.CreateDirectory(_projectDir);
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);

        private void MakeRepo(params string[] gitignore)
        {
            Directory.CreateDirectory(Path.Combine(_root, ".git"));
            if (gitignore.Length > 0)
            {
                File.WriteAllLines(Path.Combine(_root, ".gitignore"), gitignore);
            }
        }

        private void AddScreenshots() => Directory.CreateDirectory(Path.Combine(_projectDir, ScreenshotsFolderRule.FolderName));

        private IProjectModel Project()
        {
            var project = Substitute.For<IProjectModel>();
            project.Directory.Returns(_projectDir);
            return project;
        }

        /// <summary>The rule as Studio hands it to Inspect: ErrorLevel holds the configured level.</summary>
        private static Rule Configured(TraceLevel level = TraceLevel.Error)
        {
            var rule = ScreenshotsFolderRule.Get();
            rule.ErrorLevel = level;
            return rule;
        }

        [Fact]
        public void No_result_without_screenshots_folder()
        {
            MakeRepo();

            Assert.False(ScreenshotsFolderRule.Inspect(Project(), Configured()).HasErrors);
        }

        [Fact]
        public void Error_when_not_in_a_git_repo()
        {
            AddScreenshots();

            var result = ScreenshotsFolderRule.Inspect(Project(), Configured());

            Assert.True(result.HasErrors);
            Assert.Equal(TraceLevel.Error, result.ErrorLevel);
            Assert.Contains("add it to .gitignore", Assert.Single(result.InspectionMessages).Message);
        }

        [Fact]
        public void Error_when_in_repo_but_not_ignored()
        {
            MakeRepo("bin/", "obj/");
            AddScreenshots();

            var result = ScreenshotsFolderRule.Inspect(Project(), Configured());

            Assert.Equal(TraceLevel.Error, result.ErrorLevel);
        }

        [Fact]
        public void Warning_when_ignored()
        {
            MakeRepo(".screenshots/");
            AddScreenshots();

            var result = ScreenshotsFolderRule.Inspect(Project(), Configured());

            Assert.True(result.HasErrors);
            Assert.Equal(TraceLevel.Warning, result.ErrorLevel);
            Assert.Contains("is ignored by .gitignore", Assert.Single(result.InspectionMessages).Message);
        }

        [Theory]
        [InlineData(TraceLevel.Error, TraceLevel.Warning)]
        [InlineData(TraceLevel.Warning, TraceLevel.Warning)]
        [InlineData(TraceLevel.Info, TraceLevel.Info)]
        [InlineData(TraceLevel.Verbose, TraceLevel.Verbose)]
        [InlineData(TraceLevel.Off, TraceLevel.Off)]
        public void Ignored_level_is_never_above_configured_level(TraceLevel configured, TraceLevel expected)
        {
            Assert.Equal(expected, ScreenshotsFolderRule.AtMostWarning(configured));
        }

        [Fact]
        public void Configured_info_stays_info_when_ignored()
        {
            MakeRepo(".screenshots/");
            AddScreenshots();

            Assert.Equal(TraceLevel.Info, ScreenshotsFolderRule.Inspect(Project(), Configured(TraceLevel.Info)).ErrorLevel);
        }

        [Fact]
        public void Configured_warning_stays_warning_when_not_ignored()
        {
            MakeRepo();
            AddScreenshots();

            Assert.Equal(TraceLevel.Warning, ScreenshotsFolderRule.Inspect(Project(), Configured(TraceLevel.Warning)).ErrorLevel);
        }
    }
}
