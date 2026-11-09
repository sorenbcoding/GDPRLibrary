namespace SorenB.Rules.Privacy
{
    /// <summary>Builds messages in the SorenB pattern: what is wrong. Why it matters. How to fix.</summary>
    internal static class RuleMessage
    {
        internal const string DocumentationBaseUrl = "https://github.com/sorenbcoding/privacy-compliance-rules";

        public static string Format(string what, string why, string fix)
            => $"{Clean(what)}. {Clean(why)}. {Clean(fix)}.";

        /// <summary>Link to the rule's own page, e.g. docs/rules/SB-SEC-001.md.</summary>
        public static string DocumentationLink(string ruleId)
            => $"{DocumentationBaseUrl}/blob/main/docs/rules/{ruleId}.md";

        private static string Clean(string s) => s.Trim().TrimEnd('.', '!');
    }
}
