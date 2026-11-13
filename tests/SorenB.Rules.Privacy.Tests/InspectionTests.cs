using NSubstitute;
using SorenB.Rules.Privacy.Inspection;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace SorenB.Rules.Privacy.Tests
{
    public class ExpressionTests
    {
        [Theory]
        [InlineData("dtr_NyAnsøger(\"ElevNavn\").ToString.Split(\" \")(0)", new[] { "dtr_NyAnsøger", "ElevNavn", "ToString", "Split" })]
        [InlineData("\"Poster fundet: \"+Datatable.Rows.Count.ToString", new[] { "Datatable", "Rows", "Count", "ToString" })]
        [InlineData("row[\"email\"].ToString()", new[] { "row", "email", "ToString" })]
        [InlineData("\"Behandler gyldige kombinationer\"", new string[0])]
        [InlineData("String.Format(\"{0}\", cprNummer)", new[] { "String", "Format", "cprNummer" })]
        public void Names_are_identifiers_and_indexer_keys(string expression, string[] expected)
        {
            Assert.Equal(expected, Expression.Names(expression));
        }

        [Fact]
        public void Vb_doubled_quotes_stay_inside_the_literal()
        {
            Assert.Equal(new[] { "He said \"hi\" to ", }, Expression.StringLiterals("\"He said \"\"hi\"\" to \" + navn"));
            Assert.Equal(new[] { "navn" }, Expression.Names("\"He said \"\"hi\"\" to \" + navn"));
        }

        [Fact]
        public void CSharp_escapes_are_read_in_csharp_projects()
        {
            const string expression = "\"He said \\\"hi\\\" to \" + navn";

            Assert.Equal(new[] { "He said \"hi\" to " }, Expression.StringLiterals(expression, csharp: true));
            Assert.Equal(new[] { "navn" }, Expression.Names(expression, csharp: true));
        }

        [Fact]
        public void Vb_path_ending_in_backslash_closes_the_literal()
        {
            Assert.Equal(new[] { "C:\\Temp\\" }, Expression.StringLiterals("\"C:\\Temp\\\" + fileName"));
            Assert.Equal(new[] { "fileName" }, Expression.Names("\"C:\\Temp\\\" + fileName"));
        }

        [Theory]
        [InlineData("\"secret\"", false, "secret")]
        [InlineData("[\"secret\"]", false, "secret")]
        [InlineData("plain text", true, "plain text")]
        [InlineData("plain text", false, null)]
        [InlineData("[str_Password]", true, null)]
        [InlineData("\"a\" + b", false, null)]
        [InlineData("{x:Null}", true, null)]
        public void LiteralValue_only_for_plain_literals(string expression, bool isLiteral, string? expected)
        {
            Assert.Equal(expected, Expression.LiteralValue(expression, isLiteral));
        }

        [Theory]
        [InlineData("ElevNavn", new[] { "elev", "navn" })]
        [InlineData("dtr_NyAnsøger", new[] { "dtr", "ny", "ansøger" })]
        [InlineData("Holdnavn", new[] { "holdnavn" })]
        [InlineData("CPRNummer", new[] { "cpr", "nummer" })]
        [InlineData("str_Email2", new[] { "str", "email" })]
        [InlineData("ØvelseNavn", new[] { "øvelse", "navn" })]
        public void Words_split_on_case_underscore_and_digits(string identifier, string[] expected)
        {
            Assert.Equal(expected, Expression.Words(identifier));
        }
    }

    public class NameListTests
    {
        private static readonly NameList Defaults = NameList.Parse(PersonalDataNames.DefaultNames, PersonalDataNames.DefaultIgnored);

        [Theory]
        [InlineData("ElevNavn", "elevnavn")]
        [InlineData("ElevAdresse", "elev")]
        [InlineData("Fornavn", "fornavn")]
        [InlineData("dtr_NyAnsøger", "ansøger")]
        [InlineData("cprNummer", "cprnummer")]
        [InlineData("CprNr", "cprnr")]
        [InlineData("str_Email", "email")]
        [InlineData("elevnavn", "elevnavn")]
        [InlineData("VærgeEmail", "værge")]
        [InlineData("dtr_Værger", "værger")]
        [InlineData("Fravær", "fravær")]
        [InlineData("FraværsDage", "fraværs")]
        [InlineData("SygefraværProcent", "sygefravær")]
        public void Matches_personal_data_names(string identifier, string expected)
        {
            Assert.Equal(expected, Defaults.Match(identifier));
        }

        [Theory]
        [InlineData("Holdnavn")]
        [InlineData("FilNavn")]
        [InlineData("filnavn")]
        [InlineData("ArkNavn")]
        [InlineData("Datatable")]
        [InlineData("CurrentHoldnavn")]
        [InlineData("antalElever")]
        [InlineData("")]
        public void Does_not_match_other_names(string identifier)
        {
            Assert.Null(Defaults.Match(identifier));
        }

        [Fact]
        public void Empty_list_matches_nothing()
        {
            Assert.Null(NameList.Parse("").Match("cprNummer"));
        }
    }

    public class ActivityMembersTests
    {
        [Theory]
        [InlineData("UiPath.Core.Activities.LogMessage", "LogMessage")]
        [InlineData("UiPath.Core.Activities.LogMessage, UiPath.System.Activities, Version=23.10.0.0", "LogMessage")]
        [InlineData("System.Activities.Statements.Assign`1[[System.String]]", "Assign")]
        [InlineData("LogMessage", "LogMessage")]
        [InlineData("Outer+Inner", "Inner")]
        [InlineData(null, "")]
        public void ShortTypeName(string? type, string expected)
        {
            Assert.Equal(expected, ActivityMembers.ShortTypeName(type));
        }

        [Fact]
        public void Walks_nested_property_arguments()
        {
            var activity = Fakes.Activity("UiPath.Core.Activities.AddLogFields", "Add Log Fields",
                properties: new[] { Fakes.Prop("Fields", null, Fakes.Arg("Fornavn", "dtr(\"ElevNavn\").ToString")) });

            var member = Assert.Single(ActivityMembers.All(activity), m => m.Name == "Fornavn");
            Assert.Equal("Fields.Fornavn", member.Path);
        }

        [Fact]
        public void Null_collections_are_empty()
        {
            var activity = Substitute.For<IActivityModel>();

            Assert.Empty(ActivityMembers.All(activity));
        }
    }
}
