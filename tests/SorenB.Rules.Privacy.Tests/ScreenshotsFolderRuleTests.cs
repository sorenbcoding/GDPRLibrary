using SorenB.Rules.Privacy.Rules;
using NSubstitute;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    public sealed class ScreenshotsFolderRuleTests : IDisposable
    {
        private readonly string _projectDir = Path.Combine(Path.GetTempPath(), "sb-sec-002-" + Guid.NewGuid().ToString("N"));

        public ScreenshotsFolderRuleTests() => Directory.CreateDirectory(_projectDir);

        public void Dispose() => Directory.Delete(_projectDir, recursive: true);

        private IProjectModel Project()
        {
            var project = Substitute.For<IProjectModel>();
            project.Directory.Returns(_projectDir);
            return project;
        }

        [Fact]
        public void Error_when_screenshots_folder_exists()
        {
            Directory.CreateDirectory(Path.Combine(_projectDir, ".screenshots"));

            var result = ScreenshotsFolderRule.Inspect(Project(), ScreenshotsFolderRule.Get());

            Assert.True(result.HasErrors);
            Assert.Single(result.InspectionMessages);
        }

        [Fact]
        public void No_error_without_screenshots_folder()
        {
            var result = ScreenshotsFolderRule.Inspect(Project(), ScreenshotsFolderRule.Get());

            Assert.False(result.HasErrors);
        }
    }
}
