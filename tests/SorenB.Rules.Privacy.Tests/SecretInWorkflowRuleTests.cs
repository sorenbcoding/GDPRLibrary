using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;
using static SorenB.Rules.Privacy.Tests.Fakes;

namespace SorenB.Rules.Privacy.Tests
{
    public class SecretInWorkflowRuleTests
    {
        private const string HttpClientType = "UiPath.Web.Activities.HttpClient";
        private const string AssignType = "System.Activities.Statements.Assign";

        private static IReadOnlyList<string> Inspect(IActivityModel activity)
            => Messages(SecretInWorkflowRule.Inspect(activity, Configured(SecretInWorkflowRule.Get())));

        [Fact]
        public void Real_http_request_with_null_secrets_is_fine()
        {
            // <ui:HttpClient ConsumerSecret="{x:Null}" OAuth2Token="{x:Null}" Password="{x:Null}" SecurePassword="{x:Null}" … />
            var activity = Activity(HttpClientType, "HTTP Request", new[]
            {
                Arg("ConsumerSecret", "{x:Null}", true), Arg("OAuth2Token", "{x:Null}", true),
                Arg("Password", "{x:Null}", true), Arg("SecurePassword", "{x:Null}", true),
                Arg("EndPoint", "str_Endpoint + \"amu/kurser\""),
            });

            Assert.Empty(Inspect(activity));
        }

        [Theory]
        [InlineData("Password", "\"abc123\"", false)]
        [InlineData("OAuth2Token", "\"eyJhbGciOi\"", false)]
        [InlineData("ApiKey", "sk-live-123", true)]
        [InlineData("BasicAuthPassword", "[\"Sommer2026\"]", false)]
        public void Flags_fixed_value_in_secret_property(string property, string expression, bool isLiteral)
        {
            var activity = Activity(HttpClientType, "HTTP Request", new[] { Arg(property, expression, isLiteral) });

            Assert.Contains($"has a fixed value in '{property}'", Assert.Single(Inspect(activity)));
        }

        [Theory]
        [InlineData("str_Password")]
        [InlineData("GetCredential.SecurePassword")]
        [InlineData("\"\"")]
        public void Variables_assets_and_empty_values_are_fine(string expression)
        {
            Assert.Empty(Inspect(Activity(HttpClientType, "HTTP Request", new[] { Arg("Password", expression) })));
        }

        [Theory]
        [InlineData("New System.Net.NetworkCredential(\"\", \"Sommer2026\").SecurePassword")]
        [InlineData("new NetworkCredential(String.Empty, \"Sommer2026\").SecurePassword")]
        public void Flags_network_credential_from_fixed_password(string expression)
        {
            var activity = Activity("UiPath.UIAutomationNext.Activities.NTypeInto", "Type Into", new[] { Arg("SecureText", expression) });

            Assert.Contains("builds a credential from a fixed password", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Network_credential_from_variable_is_fine()
        {
            var activity = Activity("UiPath.UIAutomationNext.Activities.NTypeInto", "Type Into", new[] { Arg("SecureText", "New NetworkCredential(\"\", str_Pwd).SecurePassword") });

            Assert.Empty(Inspect(activity));
        }

        [Theory]
        [InlineData("\"Server=sql01;Database=Elever;User Id=robot;Password=Sommer2026;\"")]
        [InlineData("\"Data Source=sql01;Initial Catalog=x;uid=robot;pwd=abc\"")]
        public void Flags_connection_string_password(string expression)
        {
            var activity = Activity("UiPath.Database.Activities.DatabaseConnect", "Connect", new[] { Arg("ConnectionString", expression) });

            Assert.Contains("connection string with a password", Assert.Single(Inspect(activity)));
        }

        [Theory]
        [InlineData("\"Server=sql01;Database=Elever;Integrated Security=True;\"")]
        [InlineData("String.Format(\"Server=sql01;Password={0};\", pwd)")]
        public void Connection_string_without_fixed_password_is_fine(string expression)
        {
            Assert.Empty(Inspect(Activity("UiPath.Database.Activities.DatabaseConnect", "Connect", new[] { Arg("ConnectionString", expression) })));
        }

        [Fact]
        public void Flags_assign_of_fixed_value_to_secret_variable()
        {
            var activity = Activity(AssignType, "Assign", new[] { Arg("To", "str_Kodeord"), Arg("Value", "\"Sommer2026\"") });

            Assert.Contains("assigns a fixed value to 'str_Kodeord'", Assert.Single(Inspect(activity)));
        }

        [Theory]
        [InlineData("str_Kodeord", "GetCredential.Password")]
        [InlineData("str_TokenUrl", "\"https://login.example.dk/token\"")]
        [InlineData("str_Holdnavn", "\"3.a\"")]
        public void Assign_from_expression_url_or_other_name_is_fine(string to, string value)
        {
            Assert.Empty(Inspect(Activity(AssignType, "Assign", new[] { Arg("To", to), Arg("Value", value) })));
        }

        [Fact]
        public void Flags_secret_variable_with_fixed_default()
        {
            var activity = Activity("System.Activities.Statements.Sequence", "Main",
                variables: new[] { Variable("apiKey", "\"sk-live-123\""), Variable("str_Password", null), Variable("antal", "\"3\"") });

            Assert.Contains("Variable 'apiKey' has a fixed default value", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Message_never_repeats_the_secret()
        {
            var activity = Activity(HttpClientType, "HTTP Request", new[] { Arg("Password", "\"Sommer2026\""), Arg("Body", "\"pwd=Sommer2026\"") });

            Assert.All(Inspect(activity), m => Assert.DoesNotContain("Sommer2026", m));
        }

        [Fact]
        public void Every_message_follows_brand_voice()
        {
            var activity = Activity(AssignType, "Assign",
                new[] { Arg("To", "pwd"), Arg("Value", "\"x1\""), Arg("Password", "\"x2\""), Arg("Note", "\"Password=x3\"") },
                variables: new[] { Variable("token", "\"x4\"") });

            var messages = Inspect(activity);

            Assert.Equal(4, messages.Count);
            Assert.All(messages, BrandVoiceTests.AssertVoice);
        }
    }
}
