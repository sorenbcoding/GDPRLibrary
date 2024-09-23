using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;

namespace GDPRLibrary
{
    public class RegisterAnalyzerConfiguration : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule(gdpr_pin_check.Get());
        }
    }
}
