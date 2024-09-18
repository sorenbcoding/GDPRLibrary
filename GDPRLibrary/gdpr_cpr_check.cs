using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
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
                RecommendationMessage = "GDPR - Checks for danish personal identification number (CPR-number) inside the code.",
                ErrorLevel = System.Diagnostics.TraceLevel.Error,
            };
            rule.Parameters.Add(RegexKey, new Parameter()
        }
        private static InspectionResult Inspect(IActivityModel activityModel, Rule ruleInstance)
        {
            var setRegexValue = ruleInstance.Parameters[RegexKey]?.Value;

            var regex = new Regex(setRegexValue);
            var messageList = new List<InspectionMessage>();

            if (string.IsNullOrEmpty(setRegexValue))
            {
                return new InspectionResult()
                {
                    HasErrors = false
                };
            }

            if (!String.IsNullOrEmpty(activityModel.DisplayName) && Regex.IsMatch(activityModel.DisplayName, setRegexValue))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"The Activity '{activityModel.DisplayName}' contains CPR-number in DisplayName!",
                });
            };

            if (!String.IsNullOrEmpty(activityModel.AnnotationText) && Regex.IsMatch(activityModel.AnnotationText, setRegexValue))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"Aktiviteten '{activityModel.DisplayName}' indeholder cpr-nummer i AnnotationText!",
                });
            };

            if (activityModel.Arguments.Count == 0 & activityModel.Variables.Count == 0 & messageList.Count == 0)
            {
                return new InspectionResult()
                {
                    HasErrors = false
                };
            }

            foreach (var argument in activityModel.Arguments)
            {
                if (!String.IsNullOrEmpty(argument.DefinedExpression) && Regex.IsMatch(argument.DefinedExpression, setRegexValue))
                {
                    messageList.Add(new InspectionMessage()
                    {
                        Message = $"Aktiviteten '{activityModel.DisplayName}' indeholder cpr-nummer!",
                    });
                };
            }

            foreach (var variable in activityModel.Variables)
            {
                if (!String.IsNullOrEmpty(variable.DefaultValue) && Regex.IsMatch(variable.DefaultValue, setRegexValue))
                {
                    messageList.Add(new InspectionMessage()
                    {
                        Message = $"Variablen '{variable.DisplayName}' indeholder cpr-nummer!",
                    });
                };
            }
        }
    }
}
