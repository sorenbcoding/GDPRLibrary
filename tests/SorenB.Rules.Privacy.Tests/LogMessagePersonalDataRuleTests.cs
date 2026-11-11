using SorenB.Rules.Privacy.Inspection;
using SorenB.Rules.Privacy.Rules;
using Xunit;
using static SorenB.Rules.Privacy.Tests.Fakes;

namespace SorenB.Rules.Privacy.Tests
{
    public class LogMessagePersonalDataRuleTests
    {
        private const string LogMessageType = "UiPath.Core.Activities.LogMessage";
        private const string AddLogFieldsType = "UiPath.Core.Activities.AddLogFields";
        private const string WriteLineType = "System.Activities.Statements.WriteLine";

        private static IReadOnlyList<string> Inspect(UiPath.Studio.Analyzer.Models.IActivityModel activity)
            => Messages(LogMessagePersonalDataRule.Inspect(activity, Configured(LogMessagePersonalDataRule.Get())));

        [Fact]
        public void Flags_log_message_using_a_personal_data_variable()
        {
            var activity = Activity(LogMessageType, "Log Message", new[] { Arg("Level", "Info", true), Arg("Message", "\"Behandler \" + cprNummer") });

            Assert.Contains("logs 'cprNummer'", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Flags_indexer_key_in_log_message()
        {
            var activity = Activity(LogMessageType, "Log Message", new[] { Arg("Message", "\"Ansøger: \" + row(\"ElevNavn\").ToString") });

            Assert.Contains("logs 'ElevNavn'", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Flags_cpr_number_in_log_text()
        {
            var activity = Activity(LogMessageType, "Log Message", new[] { Arg("Message", "\"Fejl for 010190-1234\"") });

            Assert.Contains("personal identification number", Assert.Single(Inspect(activity)));
        }

        [Theory]
        [InlineData("\"Behandler gyldige kombinationer\"")]
        [InlineData("\"Poster fundet: \"+Datatable.Rows.Count.ToString")]
        [InlineData("\"Hold: \" + CurrentHoldnavn")]
        [InlineData("\"Gemmer \" + FilNavn")]
        [InlineData("\"Antal elever: \" + antalElever.ToString")]
        public void Does_not_flag_ordinary_log_messages(string message)
        {
            Assert.Empty(Inspect(Activity(LogMessageType, "Log Message", new[] { Arg("Message", message) })));
        }

        [Fact]
        public void Literal_text_names_nothing()
        {
            // Plain text that mentions "ansøger" is a description, not data.
            var activity = Activity(LogMessageType, "Log Message - ansøger", new[] { Arg("Message", "Behandler ansøger", true) });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Flags_add_log_fields_key_and_value_like_real_xaml()
        {
            // <ui:AddLogFields.Fields><InArgument x:Key="Fornavn">[dtr_NyAnsøger("ElevNavn").ToString.Split(" ")(0)]</InArgument>
            var activity = Activity(AddLogFieldsType, "Add Log Fields - info om blanket/ansøger",
                properties: new[]
                {
                    Prop("Fields", null,
                        Arg("BlanketID", "dtr_NyAnsøger(\"BlanketID\").ToString"),
                        Arg("Fornavn", "dtr_NyAnsøger(\"ElevNavn\").ToString.Split(\" \")(0)")),
                });

            var messages = Inspect(activity);

            Assert.Contains(messages, m => m.Contains("adds the log field 'Fornavn'"));
            Assert.Contains(messages, m => m.Contains("logs 'dtr_NyAnsøger'"));
        }

        [Fact]
        public void Does_not_flag_add_log_fields_without_personal_data()
        {
            var activity = Activity(AddLogFieldsType, "Add Log Fields",
                properties: new[] { Prop("Fields", null, Arg("BlanketID", "blanketId.ToString"), Arg("Holdnavn", "currentHold")) });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Flags_write_line_text()
        {
            var activity = Activity(WriteLineType, "Write Line", new[] { Arg("Text", "\"Mail til \" + str_Email") });

            Assert.Contains("logs 'str_Email'", Assert.Single(Inspect(activity)));
        }

        [Fact]
        public void Every_message_follows_brand_voice()
        {
            var messages = Inspect(Activity(AddLogFieldsType, "Add Log Fields",
                properties: new[] { Prop("Fields", null, Arg("Fornavn", "cprNummer"), Arg("Note", "\"010190-1234\"")) }));

            Assert.Equal(3, messages.Count);
            Assert.All(messages, BrandVoiceTests.AssertVoice);
        }

        [Fact]
        public void Ignores_other_activities()
        {
            var activity = Activity("UiPath.Core.Activities.Assign", "Assign", new[] { Arg("Value", "cprNummer") });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Organisation_names_extend_the_default_list()
        {
            var rule = Configured(LogMessagePersonalDataRule.Get());
            rule.Parameters[PersonalDataNames.NamesKey].Value = "skolelogin";
            var activity = Activity(LogMessageType, "Log Message", new[] { Arg("Message", "\"Login: \" + SkoleLogin") });

            Assert.Contains("logs 'SkoleLogin'", Assert.Single(Messages(LogMessagePersonalDataRule.Inspect(activity, rule))));
        }

        [Fact]
        public void Ignored_names_suppress_a_match()
        {
            var rule = Configured(LogMessagePersonalDataRule.Get());
            rule.Parameters[PersonalDataNames.IgnoredKey].Value = "studienavn";
            var activity = Activity(LogMessageType, "Log Message", new[] { Arg("Message", "\"Studie: \" + StudieNavn") });

            Assert.Empty(Messages(LogMessagePersonalDataRule.Inspect(activity, rule)));
        }
    }
}
