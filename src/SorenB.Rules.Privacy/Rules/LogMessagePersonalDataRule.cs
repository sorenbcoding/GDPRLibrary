using System.Diagnostics;
using SorenB.Rules.Privacy.Inspection;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-003: finds personal data written to robot logs by Log Message, Add Log Fields and Write Line.</summary>
    internal static class LogMessagePersonalDataRule
    {
        internal const string RuleId = "SB-SEC-003";

        private const string Why = "Robot logs are stored in Orchestrator for the whole retention period, where everyone with log access can read them";

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("Personal data in log message", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: "A Log Message, Add Log Fields or Write Line activity writes personal data to the robot log",
                    why: Why,
                    fix: "Log a case key or a count instead, and look up the person in the system that owns the data"),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
            rule.Parameters.Add(PersonalDataNames.NamesKey, new Parameter
            {
                DefaultValue = PersonalDataNames.DefaultNames,
                LocalizedDisplayName = PersonalDataNames.NamesDisplayName,
            });
            rule.Parameters.Add(PersonalDataNames.IgnoredKey, new Parameter
            {
                DefaultValue = PersonalDataNames.DefaultIgnored,
                LocalizedDisplayName = PersonalDataNames.IgnoredDisplayName,
            });

            return rule;
        }

        internal static InspectionResult Inspect(IActivityModel activity, Rule configuredRule)
        {
            var isLogMessage = ActivityMembers.IsType(activity, "LogMessage");
            var isWriteLine = ActivityMembers.IsType(activity, "WriteLine");
            var isAddLogFields = ActivityMembers.IsType(activity, "AddLogFields");
            if (!isLogMessage && !isWriteLine && !isAddLogFields)
            {
                return RuleSupport.Passed();
            }

            var names = NameList.Parse(
                RuleSupport.ParameterValue(configuredRule, PersonalDataNames.NamesKey),
                RuleSupport.ParameterValue(configuredRule, PersonalDataNames.IgnoredKey));

            var csharp = ActivityMembers.IsCSharp(activity);
            var messages = new List<string>();
            foreach (var member in ActivityMembers.All(activity))
            {
                if (isAddLogFields)
                {
                    // Every entry of Fields is a log field: its key becomes a column in Orchestrator and Insights.
                    var keyMatch = names.Match(member.Name);
                    if (keyMatch is not null)
                    {
                        messages.Add(RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' adds the log field '{member.Name}', which looks like personal data ({keyMatch})",
                            why: Why,
                            fix: "Remove the field or log a case key instead. If the name is not about a person, add it to the ignored names"));
                    }
                }
                else if (!IsLoggedText(member, isLogMessage))
                {
                    continue;
                }

                messages.AddRange(CheckExpression(activity, member, names, csharp));
            }

            return RuleSupport.Failed(configuredRule, messages);
        }

        /// <summary>Log Message writes its Message, Write Line its Text. Other members (Level, DisplayName, …) are not logged text.</summary>
        private static bool IsLoggedText(Member member, bool isLogMessage)
            => string.Equals(member.Name, isLogMessage ? "Message" : "Text", StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> CheckExpression(IActivityModel activity, Member member, NameList names, bool csharp)
        {
            if (string.Equals(member.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            // Literal text ("Processing applicant") names nothing; only an expression refers to data.
            var usedNames = member.IsLiteral ? Array.Empty<string>() : Expression.Names(member.Expression, csharp);
            foreach (var name in usedNames)
            {
                var match = names.Match(name);
                if (match is not null)
                {
                    yield return RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' logs '{name}', which looks like personal data ({match})",
                        why: Why,
                        fix: "Log a case key or a count instead. If the name is not about a person, add it to the ignored names");
                    yield break;
                }
            }

            if (Expression.StringLiterals(member.Expression, csharp).Any(CprPattern.IsMatch)
                || (member.IsLiteral && CprPattern.IsMatch(member.Expression)))
            {
                yield return RuleMessage.Format(
                    what: $"Activity '{activity.DisplayName}' logs a personal identification number",
                    why: Why,
                    fix: "Remove the number from the log text");
            }
        }
    }
}
