using SorenB.Rules.Privacy.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;
using static SorenB.Rules.Privacy.Tests.Fakes;

namespace SorenB.Rules.Privacy.Tests
{
    public class QueueItemPersonalDataRuleTests
    {
        private const string AddQueueItemType = "UiPath.Core.Activities.AddQueueItem";

        private static IReadOnlyList<string> Inspect(IActivityModel activity)
            => Messages(QueueItemPersonalDataRule.Inspect(activity, Configured(QueueItemPersonalDataRule.Get())));

        /// <summary>Like real XAML: QueueType, FolderPath, Priority as settings; ItemInformation entries as nested arguments.</summary>
        private static IActivityModel AddQueueItem(string? reference = null, params IArgumentModel[] itemInformation)
        {
            var arguments = new List<IArgumentModel>
            {
                Arg("QueueType", "\"R1752_Queue\""),
                Arg("FolderPath", "\"EUC NVS/Drift/R1752\""),
                Arg("Priority", "Normal", true),
            };
            if (reference is not null)
            {
                arguments.Add(Arg("Reference", reference));
            }

            return Activity(AddQueueItemType, "Add Queue Item", arguments, new[] { Prop("ItemInformation", null, itemInformation) });
        }

        [Fact]
        public void Does_not_flag_real_project_queue_item()
        {
            // <ui:AddQueueItem.ItemInformation><InArgument x:Key="Holdnavn">[CurrentHoldnavn]</InArgument>
            Assert.Empty(Inspect(AddQueueItem(null, Arg("Holdnavn", "CurrentHoldnavn"))));
        }

        [Fact]
        public void Flags_personal_data_field_name()
        {
            var messages = Inspect(AddQueueItem(null, Arg("ElevNavn", "row(\"Navn\").ToString")));

            Assert.Contains(messages, m => m.Contains("adds the queue item field 'ElevNavn'"));
        }

        [Fact]
        public void Flags_personal_data_value()
        {
            var message = Assert.Single(Inspect(AddQueueItem(null, Arg("Sag", "str_Cpr"))));

            Assert.Contains("puts 'str_Cpr' in the queue item (ItemInformation.Sag)", message);
        }

        [Fact]
        public void Flags_cpr_number_in_reference()
        {
            var message = Assert.Single(Inspect(AddQueueItem("\"Sag 010190-1234\"")));

            Assert.Contains("personal identification number in the queue item (Reference)", message);
        }

        [Fact]
        public void Flags_personal_data_in_reference_expression()
        {
            Assert.Contains("puts 'elevEmail'", Assert.Single(Inspect(AddQueueItem("\"Elev-\" + elevEmail"))));
        }

        [Fact]
        public void Case_key_reference_is_fine()
        {
            Assert.Empty(Inspect(AddQueueItem("\"Sag-\" + sagsId.ToString", Arg("BlanketID", "blanketId"))));
        }

        [Theory]
        [InlineData("UiPath.Core.Activities.AddTransactionItem")]
        [InlineData("UiPath.Core.Activities.BulkAddQueueItems")]
        public void Covers_transaction_and_bulk_activities(string type)
        {
            var activity = Activity(type, "Queue", new[] { Arg("QueueItemsDataTable", "dt_Ansøgere"), Arg("Reference", "cprNummer") });

            Assert.NotEmpty(Inspect(activity));
        }

        [Fact]
        public void Settings_are_not_data()
        {
            // A queue named after people is a setting, not personal data in the item.
            var activity = Activity(AddQueueItemType, "Add Queue Item", new[] { Arg("QueueType", "str_ElevQueue"), Arg("FolderPath", "folderNavn") });

            Assert.Empty(Inspect(activity));
        }

        [Fact]
        public void Ignores_other_activities()
        {
            Assert.Empty(Inspect(Activity("UiPath.Core.Activities.LogMessage", "Log", new[] { Arg("Message", "cprNummer") })));
        }

        [Fact]
        public void Every_message_follows_brand_voice()
        {
            var messages = Inspect(AddQueueItem("\"010190-1234\"", Arg("Fornavn", "str_Email")));

            Assert.Equal(3, messages.Count);
            Assert.All(messages, BrandVoiceTests.AssertVoice);
        }
    }
}
