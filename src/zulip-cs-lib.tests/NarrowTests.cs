using System;
using System.Text.Json;
using Xunit;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for the Narrow class.</summary>
    public class NarrowTests
    {
        [Fact]
        public void Narrow_Channel_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Channel, "Denmark");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("channel", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("Denmark", doc.RootElement.GetProperty("operand").GetString());
                Assert.False(doc.RootElement.GetProperty("negated").GetBoolean());
            }
        }

        [Fact]
        public void Narrow_Topic_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Topic, "testing");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("topic", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("testing", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Dm_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Dm, "user@example.org");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("dm", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("user@example.org", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Negated_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Channel, "random", negated: true);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("channel", doc.RootElement.GetProperty("operator").GetString());
                Assert.True(doc.RootElement.GetProperty("negated").GetBoolean());
            }
        }

        [Fact]
        public void Narrow_HasAttachment_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.HasAttachment);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("has", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("attachment", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_IsStarred_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.IsStarred);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("is", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("starred", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_IsUnread_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.IsUnread);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("is", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("unread", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Search_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Search, "hello world");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("search", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("hello world", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Sender_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Sender, "admin@example.org");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("sender", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("admin@example.org", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Channels_Public_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Channels);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("channels", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal("public", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_ToJsonArray_Single()
        {
            string json = Narrow.ToJsonArray(
                new Narrow(Narrow.NarrowOperator.Channel, "general"));

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
                Assert.Equal(1, doc.RootElement.GetArrayLength());
                Assert.Equal("channel", doc.RootElement[0].GetProperty("operator").GetString());
            }
        }

        [Fact]
        public void Narrow_ToJsonArray_Multiple()
        {
            string json = Narrow.ToJsonArray(
                new Narrow(Narrow.NarrowOperator.Channel, "general"),
                new Narrow(Narrow.NarrowOperator.Topic, "greetings"));

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
                Assert.Equal(2, doc.RootElement.GetArrayLength());
                Assert.Equal("channel", doc.RootElement[0].GetProperty("operator").GetString());
                Assert.Equal("topic", doc.RootElement[1].GetProperty("operator").GetString());
            }
        }

        [Fact]
        public void Narrow_ToJsonArray_Empty()
        {
            string json = Narrow.ToJsonArray();
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Narrow_Channel_IntegerId_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Channel, 180804L);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("channel", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal(JsonValueKind.Number, doc.RootElement.GetProperty("operand").ValueKind);
                Assert.Equal(180804, doc.RootElement.GetProperty("operand").GetInt64());
            }
        }

        [Fact]
        public void Narrow_Channel_StringName_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Channel, "general");
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("channel", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal(JsonValueKind.String, doc.RootElement.GetProperty("operand").ValueKind);
                Assert.Equal("general", doc.RootElement.GetProperty("operand").GetString());
            }
        }

        [Fact]
        public void Narrow_Sender_IntegerId_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Sender, 1234L);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("sender", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal(JsonValueKind.Number, doc.RootElement.GetProperty("operand").ValueKind);
                Assert.Equal(1234, doc.RootElement.GetProperty("operand").GetInt64());
            }
        }

        [Fact]
        public void Narrow_Dm_IntegerArrayIds_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Dm, new long[] { 1234, 5678 });
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("dm", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("operand").ValueKind);
                Assert.Equal(2, doc.RootElement.GetProperty("operand").GetArrayLength());
                Assert.Equal(1234, doc.RootElement.GetProperty("operand")[0].GetInt64());
                Assert.Equal(5678, doc.RootElement.GetProperty("operand")[1].GetInt64());
            }
        }

        [Fact]
        public void Narrow_Id_IntegerId_ToJson()
        {
            Narrow narrow = new Narrow(Narrow.NarrowOperator.Id, 12345L);
            string json = narrow.ToJson();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal("id", doc.RootElement.GetProperty("operator").GetString());
                Assert.Equal(JsonValueKind.Number, doc.RootElement.GetProperty("operand").ValueKind);
                Assert.Equal(12345, doc.RootElement.GetProperty("operand").GetInt64());
            }
        }

        [Fact]
        public void Narrow_ToJsonArray_MixedOperandTypes()
        {
            string json = Narrow.ToJsonArray(
                new Narrow(Narrow.NarrowOperator.Channel, 180804L),
                new Narrow(Narrow.NarrowOperator.Topic, "greetings"));

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
                Assert.Equal(2, doc.RootElement.GetArrayLength());
                Assert.Equal(JsonValueKind.Number, doc.RootElement[0].GetProperty("operand").ValueKind);
                Assert.Equal(180804, doc.RootElement[0].GetProperty("operand").GetInt64());
                Assert.Equal(JsonValueKind.String, doc.RootElement[1].GetProperty("operand").ValueKind);
                Assert.Equal("greetings", doc.RootElement[1].GetProperty("operand").GetString());
            }
        }
    }
}
