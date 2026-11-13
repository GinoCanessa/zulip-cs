using System;
using System.Collections.Generic;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>An initialize parser tests.</summary>
    public class IniParserTests
    {
        /// <summary>Uses the earliest delimiter and preserves embedded separators in values.</summary>
        /// <param name="line">The INI key/value line.</param>
        /// <param name="expectedValue">The complete trimmed value.</param>
        [Theory]
        [InlineData("key: value=with=equals", "value=with=equals")]
        [InlineData("key=value:with:colons", "value:with:colons")]
        [InlineData("key = https://example.com:8443/path?token=a=b ", "https://example.com:8443/path?token=a=b")]
        [InlineData("key: https://example.com:8443/path?token=a=b", "https://example.com:8443/path?token=a=b")]
        [InlineData("key==a::b==", "=a::b==")]
        [InlineData("key::a==b::", ":a==b::")]
        public void IniParser_MixedDelimiters_PreservesValue(string line, string expectedValue)
        {
            string contents = $"[TEST]\n#comment: ignored=value\n;comment=ignored:value\n{line}\n[another]\nkey=ignored\n";

            bool result = IniParser.TryGetSectionData(contents, "TEST", out Dictionary<string, string>? data);

            Assert.True(result);
            Assert.Single(data!);
            Assert.Equal(expectedValue, data!["key"]);
        }

        [Fact]
        public void IniParser_NoSections_ReturnFalse()
        {
            string contents = "";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string>? data);

            Assert.False(result, "Empty contents should return false");
        }

        [Fact]
        public void IniParser_EmptySection_NoValues()
        {
            string contents = "[TEST]\n\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string>? data);

            Assert.True(result, "Empty section should return true");
            Assert.Empty(data!);
        }

        [Fact]
        public void IniParser_EmptySection_OneValue()
        {
            string contents = "[TEST]\nvalue=valid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string>? data);

            Assert.True(result, "Empty section should return true");
            Assert.Single(data!);
            Assert.Contains<string>("value", data!.Keys);
            Assert.Contains<string>("valid", data!.Values);
        }

        [Fact]
        public void IniParser_EmptySection_TwoValues()
        {
            string contents = "[TEST]\nvalue=valid\nvalue2=alsoValid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string>? data);

            Assert.True(result, "Empty section should return true");
            Assert.Equal(2, data!.Count);
            Assert.Contains<string>("value", data!.Keys);
            Assert.Contains<string>("value2", data!.Keys);
            Assert.Contains<string>("valid", data!.Values);
            Assert.Contains<string>("alsoValid", data!.Values);
        }

        [Fact]
        public void IniParser_EmptySection_IgnoreComment()
        {
            string contents = "[TEST]\nvalue=valid\n#comment=ignored\nvalue2=alsoValid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string>? data);

            Assert.True(result, "Empty section should return true");
            Assert.Equal(2, data!.Count);
            Assert.Contains<string>("value", data!.Keys);
            Assert.Contains<string>("value2", data!.Keys);
            Assert.Contains<string>("valid", data!.Values);
            Assert.Contains<string>("alsoValid", data!.Values);
        }
    }
}
