using System.Diagnostics;
using SorenB.Rules.Privacy.Inspection;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-004: finds personal data put into Orchestrator queue items.</summary>
    internal static class QueueItemPersonalDataRule
    {
        internal const string RuleId = "SB-SEC-004";

        private const string Why = "Queue items are stored in Orchestrator, and the Reference is shown in queue lists and indexed for search";

        private const string Fix = "Put a case key in the queue item and look up the person when the item is processed. If the name is not about a person, add it to the ignored names";

        /// <summary>Settings of the activity itself, not data in the item.</summary>
        private static readonly HashSet<string> SettingMembers = new(StringComparer.OrdinalIgnoreCase)
        {
            "DisplayName", "QueueType", "QueueName", "FolderPath", "Priority", "DueDate", "DeferDate",
            "ServiceBaseAddress", "TimeoutMS", "ContinueOnError", "CommitType", "Result", "TransactionItem",
        };

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("Personal data in queue item", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: "An Add Queue Item, Add Transaction Item or Bulk Add Queue Items activity puts personal data in a queue item",
                    why: Why,
                    fix: "Put a case key in the queue item and look up the person when the item is processed"),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
            PersonalDataFinder.AddParameters(rule);
            return rule;
        }

        internal static InspectionResult Inspect(IActivityModel activity, Rule configuredRule)
        {
            if (!ActivityMembers.IsType(activity, "AddQueueItem", "AddTransactionItem", "BulkAddQueueItems"))
            {
                return RuleSupport.Passed();
            }

            var names = PersonalDataFinder.NamesFor(configuredRule);
            var csharp = ActivityMembers.IsCSharp(activity);
            var messages = new List<string>();
            foreach (var member in ActivityMembers.All(activity))
            {
                if (SettingMembers.Contains(member.Name))
                {
                    continue;
                }

                // ItemInformation entries: the key becomes a field of the queue item (SpecificContent).
                var keyMatch = PersonalDataFinder.InKey(member, names);
                if (keyMatch is not null)
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' adds the queue item field '{member.Name}', which looks like personal data ({keyMatch})",
                        why: Why,
                        fix: Fix));
                }

                foreach (var finding in PersonalDataFinder.InExpression(member, names, csharp))
                {
                    messages.Add(finding.IsCpr
                        ? RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' puts a personal identification number in the queue item ({member.Path})",
                            why: Why,
                            fix: "Remove the number and use a case key instead")
                        : RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' puts '{finding.Name}' in the queue item ({member.Path}), which looks like personal data ({finding.ListEntry})",
                            why: Why,
                            fix: Fix));
                }
            }

            return RuleSupport.Failed(configuredRule, messages);
        }
    }
}
