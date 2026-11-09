using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace SorenB.Rules.Privacy
{
    /// <summary>Registers the Privacy Compliance Rules with the Studio Workflow Analyzer.</summary>
    public class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            if (!workflowAnalyzerConfigService.HasFeature("WorkflowAnalyzerV4"))
            {
                return;
            }

            workflowAnalyzerConfigService.AddRule(PinInCodeRule.Get());
            workflowAnalyzerConfigService.AddRule(ScreenshotsFolderRule.Get());
        }
    }
}
