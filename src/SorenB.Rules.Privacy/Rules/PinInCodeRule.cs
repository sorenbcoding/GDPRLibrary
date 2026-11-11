using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-001: finds personal identification numbers in activity names, annotations, arguments and variable defaults.</summary>
    internal static class PinInCodeRule
    {
        internal const string RuleId = "SB-SEC-001";

        // Key kept from 1.x so existing Workflow Analyzer settings keep their configured pattern.
        internal const string RegexKey = "pin_in_code";

        /// <summary>Danish CPR number: DDMMYY, optional dash, 4 digits.</summary>
        internal const string DefaultRegex = "(0[1-9]|[12]\\d|3[01])(0[1-9]|1[0-2])\\d{2}[-]?\\d{4}";

        private const string StoredInPackage = "Values in workflow files are stored in source control, in the published package and in Orchestrator";

        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

        // One compiled Regex per configured pattern; null marks an invalid pattern.
        private static readonly ConcurrentDictionary<string, Regex?> RegexCache = new();

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("Personal identification number in workflow", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: "A workflow contains a personal identification number (PIN), by default a Danish CPR number",
                    why: StoredInPackage + ", where everyone with access can read it",
                    fix: "Remove the number and read it at runtime from a queue item, an Orchestrator asset or another secure source"),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
            rule.Parameters.Add(RegexKey, new Parameter()
            {
                DefaultValue = DefaultRegex,
                LocalizedDisplayName = "Regex for personal identification number. Default: Danish CPR number",
            });

            return rule;
        }

        internal static InspectionResult Inspect(IActivityModel activity, Rule configuredRule)
        {
            var pattern = configuredRule.Parameters[RegexKey]?.Value;

            if (string.IsNullOrEmpty(pattern))
            {
                return new InspectionResult() { HasErrors = false };
            }

            var regex = RegexCache.GetOrAdd(pattern, CreateRegex);
            if (regex is null)
            {
                return Failed(configuredRule, RuleMessage.Format(
                    what: $"Rule parameter '{RegexKey}' is not a valid regular expression: '{pattern}'",
                    why: $"{RuleId} cannot check for personal identification numbers until it is",
                    fix: "Correct the pattern in Project Settings > Workflow Analyzer, or reset it to the default"));
            }

            var messages = new List<string>();

            if (IsMatch(regex, activity.DisplayName))
            {
                messages.Add(RuleMessage.Format(
                    what: $"Activity '{activity.DisplayName}' has a personal identification number in its display name",
                    why: "Display names are written to the robot logs and shown in Orchestrator",
                    fix: "Rename the activity without the number"));
            }

            if (IsMatch(regex, activity.AnnotationText))
            {
                messages.Add(RuleMessage.Format(
                    what: $"Activity '{activity.DisplayName}' has a personal identification number in its annotation",
                    why: StoredInPackage,
                    fix: "Remove the number from the annotation"));
            }

            foreach (var argument in activity.Arguments ?? Enumerable.Empty<IArgumentModel>())
            {
                if (IsMatch(regex, argument.DefinedExpression))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Argument '{argument.DisplayName}' of activity '{activity.DisplayName}' has a personal identification number in its value",
                        why: StoredInPackage,
                        fix: "Pass the value at runtime instead, for example from a queue item or an Orchestrator asset"));
                }
            }

            foreach (var variable in activity.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                if (IsMatch(regex, variable.DefaultValue))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Variable '{variable.DisplayName}' has a personal identification number in its default value",
                        why: StoredInPackage,
                        fix: "Clear the default value and assign the number at runtime"));
                }
            }

            return messages.Count > 0
                ? Failed(configuredRule, messages.ToArray())
                : new InspectionResult() { HasErrors = false };
        }

        private static InspectionResult Failed(Rule configuredRule, params string[] messages) => new()
        {
            HasErrors = true,
            InspectionMessages = messages.Select(m => new InspectionMessage() { Message = m }).ToList(),
            RecommendationMessage = configuredRule.RecommendationMessage,
            ErrorLevel = configuredRule.ErrorLevel,
            DocumentationLink = configuredRule.DocumentationLink
        };

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
