using System.Diagnostics;
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
                ErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
        }

        internal static InspectionResult Inspect(IProjectModel project, Rule configuredRule)
        {
            if (string.IsNullOrEmpty(project.Directory)
                || !Directory.Exists(Path.Combine(project.Directory, FolderName)))
            {
                return new InspectionResult() { HasErrors = false };
            }

            return new InspectionResult()
            {
                HasErrors = true,
                InspectionMessages = new List<InspectionMessage>
                {
                    new InspectionMessage()
                    {
                        Message = RuleMessage.Format(
                            what: $"Folder {FolderName} exists in project folder '{project.Directory}'",
                            why: Why,
                            fix: $"Delete the {FolderName} folder and add it to .gitignore"),
                    }
                },
                RecommendationMessage = configuredRule.RecommendationMessage,
                ErrorLevel = configuredRule.ErrorLevel,
                DocumentationLink = configuredRule.DocumentationLink
            };
        }
    }
}
