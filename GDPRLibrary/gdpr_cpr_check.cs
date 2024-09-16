using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace GDPRLibrary
{
    internal static class gdpr_cpr_check
    {
        private const string RuleId = "SB-SEC-001";
        private const string RegexKey = "cpr_in_code";
        private const string DefaultRegex = "(0[1-9]|[12]\\d|3[01])(0[1-9]|1[0-2])\\d{2}[-]?\\d{4}";
        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("GDPR - Check for CPR number in code.", RuleId, Inspect)
            {
                RecommendationMessage = "GDPR - Check for CPR number in code.",
                ErrorLevel = System.Diagnostics.TraceLevel.Error,
            };
            return rule;
        }
    }
}
