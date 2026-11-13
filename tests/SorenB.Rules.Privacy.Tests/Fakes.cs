using NSubstitute;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Tests
{
    /// <summary>NSubstitute fakes of the analyzer model, shaped like real XAML from Studio projects.</summary>
    internal static class Fakes
    {
        internal static IActivityModel Activity(
            string type,
            string displayName,
            IEnumerable<IArgumentModel>? arguments = null,
            IEnumerable<IPropertyModel>? properties = null,
            IEnumerable<IVariableModel>? variables = null)
        {
            var activity = Substitute.For<IActivityModel>();
            activity.Type.Returns(type);
            activity.DisplayName.Returns(displayName);
            activity.Arguments.Returns((arguments ?? Array.Empty<IArgumentModel>()).ToList());
            activity.InternalArguments.Returns(new List<IArgumentModel>());
            activity.Properties.Returns((properties ?? Array.Empty<IPropertyModel>()).ToList());
            activity.InternalProperties.Returns(new List<IPropertyModel>());
            activity.Variables.Returns((variables ?? Array.Empty<IVariableModel>()).ToList());
            return activity;
        }

        /// <summary>An argument. A quoted expression ("text") is a string literal, as the model reports Literal&lt;string&gt;.</summary>
        internal static IArgumentModel Arg(string name, string? expression, bool? isLiteral = null, ArgumentDirection direction = ArgumentDirection.Unset)
        {
            var argument = Substitute.For<IArgumentModel>();
            argument.Direction.Returns(direction);
            argument.DisplayName.Returns(name);
            argument.DefinedExpression.Returns(expression);
            argument.HasLiteralExpression.Returns(isLiteral ?? (SorenB.Rules.Privacy.Inspection.Expression.LiteralValue(expression, false) is not null));
            return argument;
        }

        /// <summary>A property holding nested arguments, e.g. Add Log Fields.Fields with one argument per field key.</summary>
        internal static IPropertyModel Prop(string name, string? expression = null, params IArgumentModel[] arguments)
        {
            var property = Substitute.For<IPropertyModel>();
            property.DisplayName.Returns(name);
            property.DefinedExpression.Returns(expression);
            property.HasLiteralExpression.Returns(false);
            property.Arguments.Returns(arguments.ToList());
            property.InternalArguments.Returns(new List<IArgumentModel>());
            property.Properties.Returns(new List<IPropertyModel>());
            property.InternalProperties.Returns(new List<IPropertyModel>());
            return property;
        }

        internal static IVariableModel Variable(string name, string? defaultValue)
        {
            var variable = Substitute.For<IVariableModel>();
            variable.DisplayName.Returns(name);
            variable.DefaultValue.Returns(defaultValue);
            return variable;
        }

        /// <summary>A rule as Studio hands it to Inspect: ErrorLevel is the configured level, parameter values unset (defaults apply).</summary>
        internal static T Configured<T>(T rule) where T : Rule
        {
            rule.ErrorLevel = System.Diagnostics.TraceLevel.Error;
            return rule;
        }

        internal static IReadOnlyList<string> Messages(InspectionResult result)
            => (result.InspectionMessages ?? new List<InspectionMessage>()).Select(m => m.Message).ToList();
    }
}
