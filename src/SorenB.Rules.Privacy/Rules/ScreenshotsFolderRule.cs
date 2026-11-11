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

        // Studio leaves .screenshots out of the published package (verified in Studio), so the risk is source control.
        private const string Why = "Studio stores screenshots of the target applications there, which can show personal data, and the folder can be committed to source control";

        internal static Rule<IProjectModel> Get()
        {
            return new Rule<IProjectModel>("Screenshots folder in project", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: $"The project folder contains a {FolderName} folder",
                    why: Why,
                    fix: $"Add the {FolderName} folder to .gitignore, and delete it when you no longer need the screenshots"),
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

            // Without a repo, read the project's own .gitignore: Studio adds one to every project,
            // and git applies it as soon as the project is put under source control.
            var root = GitIgnore.FindRepoRoot(project.Directory) ?? project.Directory;
            var ignored = GitIgnore.IsIgnored(root, folder, isDirectory: true);

            var message = ignored
                ? RuleMessage.Format(
                    what: $"Folder {FolderName} exists in project folder '{project.Directory}' and is ignored by .gitignore",
                    why: "The screenshots stay out of source control and out of the published package, but they remain on this PC",
                    fix: $"Delete the {FolderName} folder when you no longer need the screenshots")
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
