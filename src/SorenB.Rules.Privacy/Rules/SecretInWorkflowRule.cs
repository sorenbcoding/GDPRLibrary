using System.Diagnostics;
using System.Text.RegularExpressions;
using SorenB.Rules.Privacy.Inspection;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Rules
{
    /// <summary>SB-SEC-006: finds passwords, API keys, tokens and connection-string passwords written into a workflow as fixed values.</summary>
    internal static class SecretInWorkflowRule
    {
        internal const string RuleId = "SB-SEC-006";
        internal const string NamesKey = "secret_names";
        internal const string DefaultNames = "password;pwd;passwd;secret;token;apikey;kodeord;adgangskode";

        private const string Why = "The published package contains the workflow as plain XAML, so everyone with access to the feed can read the secret";

        private const string Fix = "Store the secret in an Orchestrator credential asset and read it with Get Credential at run time";

        /// <summary>Activity properties that hold a secret, by name (HTTP Request, Type Into, mail and database activities).</summary>
        private static readonly HashSet<string> SecretMembers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "SecurePassword", "SecureText", "ClientSecret", "ConsumerSecret",
            "OAuth1Token", "OAuth1TokenSecret", "OAuth2Token", "OAuthToken", "ApiKey", "Token", "AccessToken",
            "ClientCertificatePassword", "SecureClientCertificatePassword", "ClientCertPassword", "ClientCertSecurePassword",
            "BasicAuthPassword", "BasicAuthSecurePassword",
        };

        // new NetworkCredential("user", "fixed").SecurePassword, New System.Net.NetworkCredential(String.Empty, "fixed")
        private const string NetworkCredentialPattern = @"NetworkCredential\s*\([^,()]*,\s*""([^""]+)""";

        // Server=…;Password=fixed;  or  Pwd=fixed  (a {0} placeholder is not a password)
        private const string ConnectionStringPattern = @"\b(?:password|pwd)\s*=\s*(?!\{)([^;""\s]+)";

        internal static Rule<IActivityModel> Get()
        {
            var rule = new Rule<IActivityModel>("Password or secret in workflow", RuleId, Inspect)
            {
                RecommendationMessage = RuleMessage.Format(
                    what: "A workflow contains a password, API key, token or connection-string password as a fixed value",
                    why: Why,
                    fix: Fix),
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = RuleMessage.DocumentationLink(RuleId)
            };
            rule.Parameters.Add(NamesKey, new Parameter
            {
                DefaultValue = DefaultNames,
                LocalizedDisplayName = "Names of variables that hold secrets (semicolon-separated). Matched against whole words, e.g. str_Password",
            });
            return rule;
        }

        internal static InspectionResult Inspect(IActivityModel activity, Rule configuredRule)
        {
            var secretNames = NameList.Parse(RuleSupport.ParameterValue(configuredRule, NamesKey));
            var csharp = ActivityMembers.IsCSharp(activity);
            var members = ActivityMembers.All(activity).ToList();
            var messages = new List<string>();

            foreach (var member in members)
            {
                if (string.Equals(member.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (SecretMembers.Contains(member.Name) && HasFixedValue(member, csharp))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' has a fixed value in '{member.Path}'",
                        why: Why,
                        fix: Fix));
                    continue;
                }

                if (Matches(NetworkCredentialPattern, member.Expression, RegexOptions.None))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' builds a credential from a fixed password ({member.Path})",
                        why: Why,
                        fix: Fix));
                }

                if (LiteralTexts(member, csharp).Any(text => Matches(ConnectionStringPattern, text, RegexOptions.IgnoreCase)))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' contains a connection string with a password ({member.Path})",
                        why: Why,
                        fix: "Store the connection string, or at least its password, in an Orchestrator credential asset"));
                }
            }

            if (ActivityMembers.IsType(activity, "Assign"))
            {
                var to = members.FirstOrDefault(m => string.Equals(m.Name, "To", StringComparison.OrdinalIgnoreCase));
                var value = members.FirstOrDefault(m => string.Equals(m.Name, "Value", StringComparison.OrdinalIgnoreCase));
                var target = Expression.Names(to.Expression, csharp).FirstOrDefault(n => secretNames.Match(n) is not null);
                if (target is not null && HasFixedValue(value, csharp))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Activity '{activity.DisplayName}' assigns a fixed value to '{target}', which looks like a secret ({secretNames.Match(target)})",
                        why: Why,
                        fix: Fix));
                }
            }

            foreach (var variable in activity.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                var entry = secretNames.Match(variable?.DisplayName);
                if (entry is not null && IsFixed(Expression.LiteralValue(variable!.DefaultValue, false, csharp)))
                {
                    messages.Add(RuleMessage.Format(
                        what: $"Variable '{variable.DisplayName}' has a fixed default value and looks like a secret ({entry})",
                        why: Why,
                        fix: Fix));
                }
            }

            return RuleSupport.Failed(configuredRule, messages);
        }

        private static bool HasFixedValue(Member member, bool csharp)
            => IsFixed(Expression.LiteralValue(member.Expression, member.IsLiteral, csharp));

        /// <summary>A non-empty literal that is not an address (tokenUrl = "https://…" is configuration, not a secret).</summary>
        private static bool IsFixed(string? literal)
            => !string.IsNullOrWhiteSpace(literal)
               && !literal.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               && !literal.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> LiteralTexts(Member member, bool csharp)
        {
            var literals = Expression.StringLiterals(member.Expression, csharp);
            return literals.Count > 0 || !member.IsLiteral || member.Expression is null ? literals : new[] { member.Expression };
        }

        private static bool Matches(string pattern, string? text, RegexOptions options)
        {
            var regex = SafeRegex.Get(pattern, RegexOptions.CultureInvariant | options);
            return regex is not null && SafeRegex.IsMatch(regex, text);
        }
    }
}
