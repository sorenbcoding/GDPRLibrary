using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace GDPRLibrary
{
    internal static class gdpr_pin_check
    {
        private const string RuleId = "SB-SEC-001";
        private const string RegexKey = "pin_in_code";
        private const string DefaultRegex = "(0[1-9]|[12]\\d|3[01])(0[1-9]|1[0-2])\\d{2}[-]?\\d{4}";
        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("GDPR check for PIN in code", RuleId, InspectForPIN)
            {
                RecommendationMessage = "GDPR:\r\nChecks for occurrences of Personal Identification Number (PIN) inside the code.\r\nThe default Regex Expression is for a Danish CPR-number.",
                ErrorLevel = System.Diagnostics.TraceLevel.Error,
                DocumentationLink = "https://gdpr.eu/"
            };
            rule.Parameters.Add(RegexKey, new Parameter() { 
                DefaultValue = DefaultRegex,
                LocalizedDisplayName = "Regex for Personal Identification Number (PIN)",
            });

            return rule;
        }
        private static InspectionResult InspectForPIN(IActivityModel activityModel, Rule ruleInstance)
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
                    Message = $"The Activity '{activityModel.DisplayName}' contains a Personal Identification Number in DisplayName!",
                });
            };

            if (!String.IsNullOrEmpty(activityModel.AnnotationText) && Regex.IsMatch(activityModel.AnnotationText, setRegexValue))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"The Activity '{activityModel.DisplayName}' contains a Personal Identification Number in AnnotationText!",
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
                        Message = $"The Argument '{activityModel.DisplayName}' contains a Personal Identification Number!",
                    });
                };
            }

            foreach (var variable in activityModel.Variables)
            {
                if (!String.IsNullOrEmpty(variable.DefaultValue) && Regex.IsMatch(variable.DefaultValue, setRegexValue))
                {
                    messageList.Add(new InspectionMessage()
                    {
                        Message = $"The Variable '{variable.DisplayName}' contains a Personal Identification Number!",
                    });
                };
            }

            if (messageList.Count > 0)
            {
                return new InspectionResult()
                {
                    HasErrors = true,
                    InspectionMessages = messageList,
                    RecommendationMessage = "Remove all Personal Identification Numbers from the code!",
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
