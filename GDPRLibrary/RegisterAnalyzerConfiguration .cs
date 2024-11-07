using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace GDPR_Compliance_Workflow_Analyzer_Rules
{
    public class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule(gdpr_pin_check.Get());
            workflowAnalyzerConfigService.AddRule(folder_check_screenshots.Get());
        }
    }
}
