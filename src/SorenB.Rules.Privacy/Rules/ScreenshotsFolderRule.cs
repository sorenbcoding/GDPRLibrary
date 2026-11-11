using System.Diagnostics;
using SorenB.Rules.Privacy.Git;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-002: finds a .screenshots folder in the project folder.</summary>
    internal static class ScreenshotsFolderRule
    {
        internal const string RuleId = "SB-SEC-002";
        internal const string FolderName = ".screenshots";

        private const string Why = "Studio stores screenshots of the target applications there, which can show personal data, and the folder can end up in source control and in the published package";

        internal static Rule<IProjectModel> Get()
        {
            return new Rule<IProjectModel>("Screenshots folder in project", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: $"The project folder contains a {FolderName} folder",
                    why: Why,
                    fix: $"Delete the {FolderName} folder before you publish, and exclude it from source control"),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
        }

        internal static InspectionResult Inspect(IProjectModel project, Rule configuredRule)
        {
            if (string.IsNullOrEmpty(project.Directory))
            {
                return new InspectionResult() { HasErrors = false };
            }

            var folder = Path.Combine(project.Directory, FolderName);
            if (!Directory.Exists(folder))
            {
                return new InspectionResult() { HasErrors = false };
            }

            var repoRoot = GitIgnore.FindRepoRoot(project.Directory);
            var ignored = repoRoot is not null && GitIgnore.IsIgnored(repoRoot, folder, isDirectory: true);

            var message = ignored
                ? RuleMessage.Format(
                    what: $"Folder {FolderName} exists in project folder '{project.Directory}' and is ignored by .gitignore",
                    why: "The screenshots stay out of source control but can still be included when the project is published",
                    fix: $"Delete the {FolderName} folder before you publish")
                : RuleMessage.Format(
                    what: $"Folder {FolderName} exists in project folder '{project.Directory}'",
                    why: Why,
                    fix: $"Delete the {FolderName} folder and add it to .gitignore");

            return new InspectionResult()
            {
                HasErrors = true,
                InspectionMessages = new List<InspectionMessage> { new InspectionMessage() { Message = message } },
                RecommendationMessage = configuredRule.RecommendationMessage,
                ErrorLevel = ignored ? AtMostWarning(configuredRule.ErrorLevel) : configuredRule.ErrorLevel,
                DocumentationLink = configuredRule.DocumentationLink
            };
        }

        /// <summary>
        /// The lower of the configured level and Warning. TraceLevel counts up as severity goes down
        /// (Error=1, Warning=2, Info=3, Verbose=4), so a configured Info stays Info. Off stays Off.
        /// </summary>
        internal static TraceLevel AtMostWarning(TraceLevel configured)
            => configured == TraceLevel.Off
                ? TraceLevel.Off
                : (TraceLevel)Math.Max((int)configured, (int)TraceLevel.Warning);
    }
}
