using NSubstitute;
using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    /// <summary>Every rule message follows the SorenB voice: what is wrong. Why it matters. How to fix.</summary>
    public sealed class BrandVoiceTests : IDisposable
    {
        private readonly string _projectDir = Path.Combine(Path.GetTempPath(), "sb-voice-" + Guid.NewGuid().ToString("N"));

        public BrandVoiceTests() => Directory.CreateDirectory(Path.Combine(_projectDir, ScreenshotsFolderRule.FolderName));

        public void Dispose() => Directory.Delete(_projectDir, recursive: true);

        /// <summary>Every rule that RegisterAnalyzerConfiguration hands to Studio.</summary>
        public static IEnumerable<object[]> Rules()
        {
            var service = Substitute.For<IAnalyzerConfigurationService>();
            service.HasFeature(Arg.Any<string>()).Returns(true);

            new RegisterAnalyzerConfiguration().Initialize(service);

            var rules = service.ReceivedCalls()
                .SelectMany(call => call.GetArguments())
                .OfType<Rule>()
                .Select(rule => new object[] { rule })
                .ToList();
            Assert.NotEmpty(rules);
            return rules;
        }

        [Theory]
        [MemberData(nameof(Rules))]
        public void Rule_metadata_follows_brand(Rule rule)
        {
            Assert.Matches("^SB-[A-Z]{3}-\\d{3}$", rule.Id);
            Assert.Equal($"https://github.com/sorenbcoding/privacy-compliance-rules/blob/main/docs/rules/{rule.Id}.md", rule.DocumentationLink);
            AssertVoice(rule.RecommendationMessage);
        }

        [Theory]
        [MemberData(nameof(Rules))]
        public void Every_rule_has_a_docs_page(Rule rule)
        {
            var page = Path.Combine(RepoRoot(), "docs", "rules", $"{rule.Id}.md");

            Assert.True(File.Exists(page), $"Missing docs page for {rule.Id}: {page}. Copy docs/rules/_template.md.");
            Assert.StartsWith($"# {rule.Id} ", File.ReadLines(page).First());
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SorenB.Rules.Privacy.sln")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new InvalidOperationException("Repo root with SorenB.Rules.Privacy.sln not found.");
        }

        [Fact]
        public void Every_violation_message_follows_brand()
        {
            var cpr = "010190-1234";
            var activity = Substitute.For<IActivityModel>();
            activity.DisplayName.Returns($"Look up {cpr}");
            activity.AnnotationText.Returns(cpr);
            var argument = Substitute.For<IArgumentModel>();
            argument.DisplayName.Returns("in_Id");
            argument.DefinedExpression.Returns(cpr);
            var variable = Substitute.For<IVariableModel>();
            variable.DisplayName.Returns("id");
            variable.DefaultValue.Returns(cpr);
            activity.Arguments.Returns(new List<IArgumentModel> { argument });
            activity.Variables.Returns(new List<IVariableModel> { variable });

            var project = Substitute.For<IProjectModel>();
            project.Directory.Returns(_projectDir);

            var configured = PinInCodeRule.Get();
            configured.Parameters[PinInCodeRule.RegexKey].Value = PinInCodeRule.DefaultRegex;
            var invalid = PinInCodeRule.Get();
            invalid.Parameters[PinInCodeRule.RegexKey].Value = "([0-9";

            var messages = PinInCodeRule.Inspect(activity, configured).InspectionMessages
                .Concat(PinInCodeRule.Inspect(activity, invalid).InspectionMessages)
                .Concat(ScreenshotsFolderRule.Inspect(project, ScreenshotsFolderRule.Get()).InspectionMessages)
                .Select(m => m.Message)
                .ToList();

            Assert.Equal(6, messages.Count);
            Assert.All(messages, AssertVoice);
        }

        private static void AssertVoice(string message)
        {
            Assert.DoesNotContain("!", message);
            Assert.EndsWith(".", message);
            // what. why. fix. -> at least three sentences.
            Assert.True(message.Split(". ").Length >= 3, $"Expected what/why/fix sentences: {message}");
        }
    }
}
