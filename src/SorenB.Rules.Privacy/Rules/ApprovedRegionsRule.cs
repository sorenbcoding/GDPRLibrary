using System.Diagnostics;
using SorenB.Rules.Privacy.Inspection;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-005: finds web addresses written into a workflow whose host is outside the approved domains (data residency).</summary>
    internal static class ApprovedRegionsRule
    {
        internal const string RuleId = "SB-SEC-005";
        internal const string DomainsKey = "approved_domains";
        internal const string DefaultDomains = "*.dk;*.eu;*.uipath.com";

        private const string Why = "Data sent to a service outside the approved regions may leave the EU without a data processing agreement";

        private const string Fix = "Use a service in an approved region, or have the domain approved and added to approved_domains in your Automation Ops policy";

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("Call to service outside approved regions", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: "A workflow contains the address of a service whose domain is not on the approved list",
                    why: Why,
                    fix: Fix),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
            rule.Parameters.Add(DomainsKey, new Parameter
            {
                DefaultValue = DefaultDomains,
                LocalizedDisplayName = "Approved domains (semicolon-separated). *.dk approves dk and every subdomain. Intranet hosts are always approved",
            });
            return rule;
        }

        internal static InspectionResult Inspect(IActivityModel activity, Rule configuredRule)
        {
            var approved = DomainList.Parse(RuleSupport.ParameterValue(configuredRule, DomainsKey));
            var csharp = ActivityMembers.IsCSharp(activity);
            var messages = new List<string>();
            var reported = new HashSet<string>(StringComparer.Ordinal);

            foreach (var member in ActivityMembers.All(activity))
            {
                if (string.Equals(member.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var host in Hosts(member.Expression, member.IsLiteral, csharp))
                {
                    if (!approved.IsApproved(host) && reported.Add(host))
                    {
                        messages.Add(RuleMessage.Format(
                            what: $"Activity '{activity.DisplayName}' uses the address '{host}' ({member.Path}), which is not on the approved domains list",
                            why: Why,
                            fix: Fix));
                    }
                }
            }

            foreach (var variable in activity.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                foreach (var host in Hosts(variable?.DefaultValue, false, csharp))
                {
                    if (!approved.IsApproved(host) && reported.Add(host))
                    {
                        messages.Add(RuleMessage.Format(
                            what: $"Variable '{variable!.DisplayName}' holds the address '{host}', which is not on the approved domains list",
                            why: Why,
                            fix: Fix));
                    }
                }
            }

            return RuleSupport.Failed(configuredRule, messages);
        }

        /// <summary>Hosts in the literal text of an expression. An address built only from variables or assets can't be resolved here.</summary>
        private static IEnumerable<string> Hosts(string? expression, bool isLiteral, bool csharp)
        {
            var texts = Expression.StringLiterals(expression, csharp).ToList();
            if (texts.Count == 0 && isLiteral && expression is not null)
            {
                texts.Add(expression); // unquoted literal, e.g. Url="https://app.studica.dk/…" in XAML
            }

            return texts.SelectMany(DomainList.HostsIn).Distinct(StringComparer.Ordinal);
        }
    }
}
