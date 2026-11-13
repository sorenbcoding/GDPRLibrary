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
            PersonalDataFinder.AddParameters(rule);
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

            var names = PersonalDataFinder.NamesFor(configuredRule);
            var csharp = ActivityMembers.IsCSharp(activity);
            var messages = new List<string>();
            foreach (var member in ActivityMembers.All(activity))
            {
                if (isAddLogFields)
                {
                    // Every entry of Fields is a log field: its key becomes a column in Orchestrator and Insights.
                    var keyMatch = PersonalDataFinder.InKey(member, names);
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

                foreach (var finding in PersonalDataFinder.InExpression(member, names, csharp))
                {
                    messages.Add(finding.IsCpr
                        ? RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' logs a personal identification number",
                            why: Why,
                            fix: "Remove the number from the log text")
                        : RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' logs '{finding.Name}', which looks like personal data ({finding.ListEntry})",
                            why: Why,
                            fix: "Log a case key or a count instead. If the name is not about a person, add it to the ignored names"));
                }
            }

            return RuleSupport.Failed(configuredRule, messages);
        }

        /// <summary>Log Message writes its Message, Write Line its Text. Other members (Level, DisplayName, …) are not logged text.</summary>
        private static bool IsLoggedText(Member member, bool isLogMessage)
            => string.Equals(member.Name, isLogMessage ? "Message" : "Text", StringComparison.OrdinalIgnoreCase);
    }
}
