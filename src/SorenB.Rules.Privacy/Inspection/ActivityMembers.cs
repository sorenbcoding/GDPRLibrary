using UiPath.Studio.Analyzer.Models;

namespace SorenB.Rules.Privacy.Inspection
{
    /// <summary>
    /// One argument or property of an activity, flattened. Path is the dotted route, e.g. "Fields.Fornavn".
    /// Direction is set for arguments (In, Out, InOut) and null for properties.
    /// </summary>
    internal readonly record struct Member(string Path, string Name, string? Expression, bool IsLiteral, ArgumentDirection? Direction = null)
    {
        /// <summary>The path of the property that holds this member, e.g. "AssignOperations.AssignOperation" for a Multiple Assign pair.</summary>
        internal string Parent => Path.Length > Name.Length && Path.EndsWith(Name, StringComparison.Ordinal) ? Path.Substring(0, Path.Length - Name.Length - 1) : string.Empty;
    }

    /// <summary>
    /// Walks an activity's arguments and properties, including non-browsable and nested ones,
    /// so values are found wherever Studio's model puts them (for example dictionary entries such as Add Log Fields fields).
    /// </summary>
    internal static class ActivityMembers
    {
        private const int MaxDepth = 4;

        internal static IEnumerable<Member> All(IActivityModel activity)
        {
            foreach (var member in Arguments(activity.Arguments, string.Empty))
            {
                yield return member;
            }

            foreach (var member in Arguments(activity.InternalArguments, string.Empty))
            {
                yield return member;
            }

            foreach (var member in Properties(activity.Properties, string.Empty, 0))
            {
                yield return member;
            }

            foreach (var member in Properties(activity.InternalProperties, string.Empty, 0))
            {
                yield return member;
            }
        }

        /// <summary>
        /// True if the activity's type has one of the given short names, e.g. "LogMessage".
        /// Robust to namespaces, assembly-qualified names, generic arity (`1) and nested types (+).
        /// </summary>
        internal static bool IsType(IActivityModel activity, params string[] shortNames)
        {
            var shortName = ShortTypeName(activity.Type);
            return shortName.Length > 0 && shortNames.Any(n => string.Equals(n, shortName, StringComparison.Ordinal));
        }

        /// <summary>True if the activity's project uses C# expressions (project.json expressionLanguage); VB otherwise.</summary>
        internal static bool IsCSharp(IActivityModel activity)
            => activity.Context?.Project?.ExpressionLanguage?.IndexOf("CSharp", StringComparison.OrdinalIgnoreCase) >= 0;

        internal static string ShortTypeName(string? type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return string.Empty;
            }

            var end = type.IndexOfAny(new[] { ',', '`', '[', '<' });
            var name = end >= 0 ? type.Substring(0, end) : type;
            var start = name.LastIndexOfAny(new[] { '.', '+' });
            return (start >= 0 ? name.Substring(start + 1) : name).Trim();
        }

        private static IEnumerable<Member> Arguments(IEnumerable<IArgumentModel>? arguments, string prefix)
        {
            foreach (var argument in arguments ?? Enumerable.Empty<IArgumentModel>())
            {
                if (argument is null)
                {
                    continue;
                }

                var name = argument.DisplayName ?? string.Empty;
                yield return new Member(Join(prefix, name), name, argument.DefinedExpression, argument.HasLiteralExpression == true, argument.Direction);
            }
        }

        private static IEnumerable<Member> Properties(IEnumerable<IPropertyModel>? properties, string prefix, int depth)
        {
            foreach (var property in properties ?? Enumerable.Empty<IPropertyModel>())
            {
                if (property is null)
                {
                    continue;
                }

                var name = property.DisplayName ?? string.Empty;
                var path = Join(prefix, name);
                yield return new Member(path, name, property.DefinedExpression, property.HasLiteralExpression == true);

                if (depth >= MaxDepth)
                {
                    continue;
                }

                foreach (var member in Arguments(property.Arguments, path))
                {
                    yield return member;
                }

                foreach (var member in Arguments(property.InternalArguments, path))
                {
                    yield return member;
                }

                foreach (var member in Properties(property.Properties, path, depth + 1))
                {
                    yield return member;
                }

                foreach (var member in Properties(property.InternalProperties, path, depth + 1))
                {
                    yield return member;
                }
            }
        }

        private static string Join(string prefix, string name) => prefix.Length == 0 ? name : $"{prefix}.{name}";
    }
}
