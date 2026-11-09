using SorenB.Rules.Privacy.Rules;
using NSubstitute;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    public class PinInCodeRuleTests
    {
        private const string Cpr = "010190-1234";
        private const string CprNoDash = "0101901234";

        private static IActivityModel Activity(
            string displayName = "Assign",
            string? annotation = null,
            IEnumerable<IArgumentModel>? arguments = null,
            IEnumerable<IVariableModel>? variables = null)
        {
            var activity = Substitute.For<IActivityModel>();
            activity.DisplayName.Returns(displayName);
            activity.AnnotationText.Returns(annotation);
            activity.Arguments.Returns((arguments ?? Array.Empty<IArgumentModel>()).ToList());
            activity.Variables.Returns((variables ?? Array.Empty<IVariableModel>()).ToList());
            return activity;
        }

        private static IArgumentModel Argument(string name, string expression)
        {
            var argument = Substitute.For<IArgumentModel>();
            argument.DisplayName.Returns(name);
            argument.DefinedExpression.Returns(expression);
            return argument;
        }

        private static IVariableModel Variable(string name, string defaultValue)
        {
            var variable = Substitute.For<IVariableModel>();
            variable.DisplayName.Returns(name);
            variable.DefaultValue.Returns(defaultValue);
            return variable;
        }

        private static UiPath.Studio.Activities.Api.Analyzer.Rules.Rule RuleWithRegex(string? regex)
        {
            var rule = PinInCodeRule.Get();
            rule.Parameters[PinInCodeRule.RegexKey].Value = regex;
            return rule;
        }

        private static UiPath.Studio.Activities.Api.Analyzer.Rules.Rule DefaultRule()
            => RuleWithRegex(PinInCodeRule.DefaultRegex);

        [Theory]
        [InlineData(Cpr)]
        [InlineData(CprNoDash)]
        public void Finds_pin_in_display_name(string pin)
        {
            var result = PinInCodeRule.Inspect(Activity(displayName: $"Look up {pin}"), DefaultRule());

            Assert.True(result.HasErrors);
            Assert.Contains("display name", Assert.Single(result.InspectionMessages).Message);
        }

        [Fact]
        public void Finds_pin_in_annotation()
        {
            var result = PinInCodeRule.Inspect(Activity(annotation: $"Test with {Cpr}"), DefaultRule());

            Assert.True(result.HasErrors);
            Assert.Contains("annotation", Assert.Single(result.InspectionMessages).Message);
        }

        [Fact]
        public void Finds_pin_in_argument_and_names_the_argument()
        {
            var activity = Activity(displayName: "Invoke Lookup", arguments: new[] { Argument("in_CustomerId", $"\"{Cpr}\"") });

            var result = PinInCodeRule.Inspect(activity, DefaultRule());

            Assert.True(result.HasErrors);
            var message = Assert.Single(result.InspectionMessages).Message;
            Assert.Contains("in_CustomerId", message);
            Assert.Contains("Invoke Lookup", message);
        }

        [Fact]
        public void Finds_pin_in_variable_default()
        {
            var activity = Activity(variables: new[] { Variable("CustomerCpr", $"\"{Cpr}\"") });

            var result = PinInCodeRule.Inspect(activity, DefaultRule());

            Assert.True(result.HasErrors);
            Assert.Contains("CustomerCpr", Assert.Single(result.InspectionMessages).Message);
        }

        [Fact]
        public void Reports_every_location()
        {
            var activity = Activity(
                displayName: $"Look up {Cpr}",
                annotation: Cpr,
                arguments: new[] { Argument("in_Id", Cpr) },
                variables: new[] { Variable("id", Cpr) });

            var result = PinInCodeRule.Inspect(activity, DefaultRule());

            Assert.Equal(4, result.InspectionMessages.Count);
        }

        [Theory]
        [InlineData("Assign")]
        [InlineData("Invoice 2024-0001")]
        [InlineData("Day 32 month 13: 3213901234")]
        public void No_error_without_pin(string displayName)
        {
            var activity = Activity(displayName: displayName, arguments: new[] { Argument("in_Amount", "1250") });

            var result = PinInCodeRule.Inspect(activity, DefaultRule());

            Assert.False(result.HasErrors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Empty_regex_skips_the_rule(string? regex)
        {
            var result = PinInCodeRule.Inspect(Activity(displayName: Cpr), RuleWithRegex(regex));

            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Invalid_regex_returns_one_message_instead_of_throwing()
        {
            var result = PinInCodeRule.Inspect(Activity(displayName: Cpr), RuleWithRegex("([0-9"));

            Assert.True(result.HasErrors);
            Assert.Contains(PinInCodeRule.RegexKey, Assert.Single(result.InspectionMessages).Message);
        }

        [Fact]
        public void Null_collections_do_not_throw()
        {
            var activity = Substitute.For<IActivityModel>();
            activity.DisplayName.Returns("Assign");
            activity.Arguments.Returns((List<IArgumentModel>)null!);
            activity.Variables.Returns((List<IVariableModel>)null!);

            var result = PinInCodeRule.Inspect(activity, DefaultRule());

            Assert.False(result.HasErrors);
        }
    }
}
