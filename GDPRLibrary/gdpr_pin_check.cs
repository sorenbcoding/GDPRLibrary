using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace GDPR_Compliance_Workflow_Analyzer_Rules
{
    internal static class gdpr_pin_check
    {
        internal const string RuleId = "SB-SEC-001";
        internal const string RegexKey = "pin_in_code";
        internal const string DefaultRegex = "(0[1-9]|[12]\\d|3[01])(0[1-9]|1[0-2])\\d{2}[-]?\\d{4}";
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        // One compiled Regex per configured pattern; null marks an invalid pattern.
        private static readonly ConcurrentDictionary<string, Regex?> RegexCache = new();

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("GDPR check for PIN in code", RuleId, InspectForPIN)
            {
                RecommendationMessage = "GDPR:\r\nChecks for occurrences of Personal Identification Number (PIN) inside the code.\r\nThe default Regex Expression is for a Danish CPR-number.",
                ErrorLevel = System.Diagnostics.TraceLevel.Error,
                DocumentationLink = "https://gdpr.eu/"
            };
            rule.Parameters.Add(RegexKey, new Parameter()
            {
                DefaultValue = DefaultRegex,
                LocalizedDisplayName = "Regex for Personal Identification Number (PIN)",
            });

            return rule;
        }

        internal static InspectionResult InspectForPIN(IActivityModel activityModel, Rule ruleInstance)
        {
            var pattern = ruleInstance.Parameters[RegexKey]?.Value;

            if (string.IsNullOrEmpty(pattern))
            {
                return new InspectionResult() { HasErrors = false };
            }

            var regex = RegexCache.GetOrAdd(pattern, CreateRegex);
            if (regex is null)
            {
                return new InspectionResult()
                {
                    HasErrors = true,
                    InspectionMessages = new List<InspectionMessage>
                    {
                        new InspectionMessage()
                        {
                            Message = $"The rule parameter '{RegexKey}' is not a valid regular expression: '{pattern}'. Correct it in the Workflow Analyzer settings.",
                        }
                    },
                    RecommendationMessage = "Correct the regular expression in the rule parameter.",
                    ErrorLevel = ruleInstance.ErrorLevel,
                    DocumentationLink = "https://gdpr.eu/"
                };
            }

            var messageList = new List<InspectionMessage>();

            if (IsMatch(regex, activityModel.DisplayName))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"The Activity '{activityModel.DisplayName}' contains a Personal Identification Number in DisplayName!",
                });
            }

            if (IsMatch(regex, activityModel.AnnotationText))
            {
                messageList.Add(new InspectionMessage()
                {
                    Message = $"The Activity '{activityModel.DisplayName}' contains a Personal Identification Number in AnnotationText!",
                });
            }

            foreach (var argument in activityModel.Arguments ?? Enumerable.Empty<IArgumentModel>())
            {
                if (IsMatch(regex, argument.DefinedExpression))
                {
                    messageList.Add(new InspectionMessage()
                    {
                        Message = $"The Argument '{argument.DisplayName}' in Activity '{activityModel.DisplayName}' contains a Personal Identification Number!",
                    });
                }
            }

            foreach (var variable in activityModel.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                if (IsMatch(regex, variable.DefaultValue))
                {
                    messageList.Add(new InspectionMessage()
                    {
                        Message = $"The Variable '{variable.DisplayName}' contains a Personal Identification Number!",
                    });
                }
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

            return new InspectionResult() { HasErrors = false };
        }

        private static Regex? CreateRegex(string pattern)
        {
            try
            {
                return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, MatchTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static bool IsMatch(Regex regex, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                return regex.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
