using SorenB.Rules.Privacy.Inspection;
using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;
using static SorenB.Rules.Privacy.Tests.Fakes;

namespace SorenB.Rules.Privacy.Tests
{
    public class DomainListTests
    {
        private static readonly DomainList Defaults = DomainList.Parse(ApprovedRegionsRule.DefaultDomains);

        [Theory]
        [InlineData("app.studica.dk")]
        [InlineData("borger.dk")]
        [InlineData("dk")]
        [InlineData("europa.eu")]
        [InlineData("cloud.uipath.com")]
        [InlineData("BORGER.DK")]
        [InlineData("intranet")]
        [InlineData("localhost")]
        [InlineData("10.1.2.3")]
        [InlineData("172.20.0.5")]
        [InlineData("192.168.1.10")]
        [InlineData("127.0.0.1")]
        [InlineData("[::1]")]
        public void Approves_eu_dk_uipath_and_internal_hosts(string host)
        {
            Assert.True(Defaults.IsApproved(host));
        }

        [Theory]
        [InlineData("api.example.com")]
        [InlineData("graph.microsoft.com")]
        [InlineData("dk.example.com")]
        [InlineData("notuipath.com")]
        [InlineData("8.8.8.8")]
        [InlineData("172.32.0.1")]
        public void Rejects_other_hosts(string host)
        {
            Assert.False(Defaults.IsApproved(host));
        }

        [Fact]
        public void Exact_entry_approves_only_that_host()
        {
            var list = DomainList.Parse("api.example.com");

            Assert.True(list.IsApproved("api.example.com"));
            Assert.False(list.IsApproved("www.example.com"));
        }

        [Theory]
        [InlineData("Se https://app.studica.dk/programmes/x?y=1 nu", new[] { "app.studica.dk" })]
        [InlineData("http://intranet/a and https://api.example.com:8443/v1", new[] { "intranet", "api.example.com" })]
        [InlineData("https://{0}.example.com/x", new string[0])]
        [InlineData("no address here", new string[0])]
        [InlineData("(see https://borger.dk).", new[] { "borger.dk" })]
        public void Finds_hosts_in_text(string text, string[] expected)
        {
            Assert.Equal(expected, DomainList.HostsIn(text));
        }
    }

    public class ApprovedRegionsRuleTests
    {
        private static IReadOnlyList<string> Inspect(IActivityModel activity, string? domains = null)
        {
            var rule = Configured(ApprovedRegionsRule.Get());
            if (domains is not null)
            {
                rule.Parameters[ApprovedRegionsRule.DomainsKey].Value = domains;
            }

            return Messages(ApprovedRegionsRule.Inspect(activity, rule));
        }

        [Fact]
        public void Flags_http_request_to_unapproved_host()
        {
            var activity = Activity("UiPath.Web.Activities.Http.NetHttpRequest", "HTTP Request", new[] { Arg("RequestUrl", "\"https://api.example.com/v1/students\"") });

            Assert.Contains("uses the address 'api.example.com' (RequestUrl)", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Approved_real_project_addresses_pass()
        {
            // <ui:NavigateTo Url="https://app.studica.dk/programmes/educational-programmes/NotStarted" />
            var activity = Activity("UiPath.Core.Activities.NavigateTo", "Navigate To", new[] { Arg("Url", "https://app.studica.dk/programmes/educational-programmes/NotStarted", true) });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Flags_unquoted_literal_url()
        {
            var activity = Activity("UiPath.UIAutomationNext.Activities.NGoToUrl", "Go To URL", new[] { Arg("Url", "https://translate.example.com/", true) });

            Assert.Single(Inspect(activity));
        }

        [Fact]
        public void Flags_literal_part_of_a_built_address()
        {
            var activity = Activity("UiPath.Web.Activities.HttpClient", "HTTP Request", new[] { Arg("EndPoint", "\"https://api.example.com/amu/kurser?fra=\" + str_APIDato") });

            Assert.Single(Inspect(activity));
        }

        [Fact]
        public void Address_from_a_variable_is_not_resolved()
        {
            // [str_BaseURL+"/cvr-permanent/virksomhed/_search"]: the host lives in str_BaseURL, checked where it is set.
            var activity = Activity("UiPath.Web.Activities.Http.NetHttpRequest", "HTTP Request", new[] { Arg("RequestUrl", "str_BaseURL+\"/cvr-permanent/virksomhed/_search\"") });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Flags_variable_default_and_assign_value()
        {
            var activity = Activity("System.Activities.Statements.Sequence", "Main",
                variables: new[] { Variable("str_BaseURL", "\"https://distribution.virk.example.com\"") });
            var assign = Activity("System.Activities.Statements.Assign", "Assign", new[] { Arg("To", "str_Url"), Arg("Value", "\"https://files.example.org/upload\"") });

            Assert.Contains("Variable 'str_BaseURL' holds the address 'distribution.virk.example.com'", Assert.Single(Inspect(activity)));
            Assert.Contains("'files.example.org' (Value)", Assert.Single(Inspect(assign)));
        }

        [Fact]
        public void Organisation_list_approves_a_domain()
        {
            var activity = Activity("UiPath.Web.Activities.Http.NetHttpRequest", "HTTP Request", new[] { Arg("RequestUrl", "\"https://graph.microsoft.com/v1.0/me\"") });

            Assert.Single(Inspect(activity));
            Assert.Empty(Inspect(activity, "*.dk;*.eu;graph.microsoft.com"));
        }

        [Fact]
        public void Reports_each_host_once_per_activity()
        {
            var activity = Activity("UiPath.Web.Activities.Http.NetHttpRequest", "HTTP Request",
                new[] { Arg("RequestUrl", "\"https://api.example.com/a\""), Arg("Body", "\"see https://api.example.com/b\"") });

            Assert.Single(Inspect(activity));
        }

        [Fact]
        public void Display_name_is_not_an_address()
        {
            var activity = Activity("UiPath.Core.Activities.LogMessage", "Log https://api.example.com", new[] { Arg("DisplayName", "\"https://api.example.com\"") });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Every_message_follows_brand_voice()
        {
            var activity = Activity("System.Activities.Statements.Sequence", "Main",
                new[] { Arg("Value", "\"https://api.example.com/x\"") },
                variables: new[] { Variable("str_Other", "\"https://other.example.org\"") });

            var messages = Inspect(activity);

            Assert.Equal(2, messages.Count);
            Assert.All(messages, BrandVoiceTests.AssertVoice);
        }
    }
}
