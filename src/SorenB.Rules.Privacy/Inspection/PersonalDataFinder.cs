namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>A name or a CPR number found in one member's expression.</summary>
    internal readonly record struct PersonalDataFinding(string? Name, string? ListEntry, bool IsCpr);

    /// <summary>Finds personal data in an activity member: a name from the list, or a CPR number in literal text.</summary>
    internal static class PersonalDataFinder
    {
        /// <summary>
        /// The first listed name the expression uses, then any CPR number in its literal text.
        /// Literal text ("Behandler ansøger") names nothing, so only its CPR numbers count. DisplayName is never data.
        /// </summary>
        internal static IEnumerable<PersonalDataFinding> InExpression(Member member, NameList names, bool csharp)
        {
            if (string.Equals(member.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            var usedNames = member.IsLiteral ? Array.Empty<string>() : Expression.Names(member.Expression, csharp);
            foreach (var name in usedNames)
            {
                var match = names.Match(name);
                if (match is not null)
                {
                    yield return new PersonalDataFinding(name, match, false);
                    break;
                }
            }

            if (Expression.StringLiterals(member.Expression, csharp).Any(CprPattern.IsMatch)
                || (member.IsLiteral && CprPattern.IsMatch(member.Expression)))
            {
                yield return new PersonalDataFinding(null, null, true);
            }
        }

        /// <summary>The list entry a member's own name matches, e.g. a dictionary key such as an Add Log Fields field or a queue item field.</summary>
        internal static string? InKey(Member member, NameList names) => names.Match(member.Name);

        internal static NameList NamesFor(UiPath.Studio.Activities.Api.Analyzer.Rules.Rule configuredRule)
            => NameList.Parse(
                RuleSupport.ParameterValue(configuredRule, PersonalDataNames.NamesKey),
                RuleSupport.ParameterValue(configuredRule, PersonalDataNames.IgnoredKey));

        /// <summary>Adds the two shared personal-data parameters to a rule.</summary>
        internal static void AddParameters(UiPath.Studio.Activities.Api.Analyzer.Rules.Rule rule)
        {
            rule.Parameters.Add(PersonalDataNames.NamesKey, new UiPath.Studio.Activities.Api.Analyzer.Rules.Parameter
            {
                DefaultValue = PersonalDataNames.DefaultNames,
                LocalizedDisplayName = PersonalDataNames.NamesDisplayName,
            });
            rule.Parameters.Add(PersonalDataNames.IgnoredKey, new UiPath.Studio.Activities.Api.Analyzer.Rules.Parameter
            {
                DefaultValue = PersonalDataNames.DefaultIgnored,
                LocalizedDisplayName = PersonalDataNames.IgnoredDisplayName,
            });
        }
    }
}
