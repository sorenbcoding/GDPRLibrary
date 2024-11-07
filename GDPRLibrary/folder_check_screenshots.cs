using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace GDPR_Compliance_Workflow_Analyzer_Rules
{
    internal static class folder_check_screenshots
    {
        private const string RuleId = "SB-SEC-002";
        internal static Rule<IProjectModel> Get()
        {
            var rule = new Rule<IProjectModel>("GDPR check for .screenshots folder", RuleId, InspectForScreenshotsFolder)
            {
                RecommendationMessage = "GDPR:\r\nChecks for presence of the .screenshots-folder inside the project folder.",
                ErrorLevel = System.Diagnostics.TraceLevel.Error,
                DocumentationLink = "https://gdpr.eu/"
            };

            return rule;
        }
        private static InspectionResult InspectForScreenshotsFolder(IProjectModel projectToInspect, Rule ruleInstance)
        {
            var messageList = new List<InspectionMessage>();

            if (Directory.Exists(Path.Combine(projectToInspect.Directory, ".screenshots")))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"The .screenshots-folder is present in the project-folder!",
                });
            }

            if (messageList.Count > 0)
            {
                return new InspectionResult()
                {
                    HasErrors = true,
                    InspectionMessages = messageList,
                    RecommendationMessage = "Delete the .screenshots-folder inside the project folder to ensure GDPR compliance!",
                    ErrorLevel = ruleInstance.ErrorLevel,
                    DocumentationLink = "https://gdpr.eu/"
                };
            }

            return new InspectionResult()
            {
                HasErrors = false
            };
        }
    }
}
