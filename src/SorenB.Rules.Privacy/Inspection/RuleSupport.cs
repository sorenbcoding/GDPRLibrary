using UiPath.Studio.Activities.Api.Analyzer.Rules;

namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>Parameter and result helpers shared by the rules.</summary>
    internal static class RuleSupport
    {
        /// <summary>The configured value, or the parameter's default when Studio has not set one. Empty means "configured empty".</summary>
        internal static string? ParameterValue(Rule rule, string key)
        {
            if (!rule.Parameters.TryGetValue(key, out var parameter) || parameter is null)
            {
                return null;
            }

            return parameter.Value ?? parameter.DefaultValue;
        }

        internal static InspectionResult Passed() => new() { HasErrors = false };

        internal static InspectionResult Failed(Rule configuredRule, IEnumerable<string> messages)
        {
            var list = messages.Distinct(StringComparer.Ordinal).ToList();
            if (list.Count == 0)
            {
                return Passed();
            }

            return new InspectionResult
            {
                HasErrors = true,
                InspectionMessages = list.Select(m => new InspectionMessage { Message = m }).ToList(),
                RecommendationMessage = configuredRule.RecommendationMessage,
                ErrorLevel = configuredRule.ErrorLevel,
                DocumentationLink = configuredRule.DocumentationLink
            };
        }
    }
}
