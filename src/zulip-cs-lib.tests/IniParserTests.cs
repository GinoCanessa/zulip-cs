using System;
using System.Collections.Generic;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>An initialize parser tests.</summary>
    public class IniParserTests
    {
        [Fact]
        public void IniParser_NoSections_ReturnFalse()
        {
            string contents = "";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string> data);

            Assert.False(result, "Empty contents should return false");
        }

        [Fact]
        public void IniParser_EmptySection_NoValues()
        {
            string contents = "[TEST]\n\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string> data);

            Assert.True(result, "Empty section should return true");
            Assert.Empty(data);
        }

        [Fact]
        public void IniParser_EmptySection_OneValue()
        {
            string contents = "[TEST]\nvalue=valid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string> data);

            Assert.True(result, "Empty section should return true");
            Assert.Single(data);
            Assert.Contains<string>("value", data.Keys);
            Assert.Contains<string>("valid", data.Values);
        }

        [Fact]
        public void IniParser_EmptySection_TwoValues()
        {
            string contents = "[TEST]\nvalue=valid\nvalue2=alsoValid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string> data);

            Assert.True(result, "Empty section should return true");
            Assert.Equal(2, data.Count);
            Assert.Contains<string>("value", data.Keys);
            Assert.Contains<string>("value2", data.Keys);
            Assert.Contains<string>("valid", data.Values);
            Assert.Contains<string>("alsoValid", data.Values);
        }

        [Fact]
        public void IniParser_EmptySection_IgnoreComment()
        {
            string contents = "[TEST]\nvalue=valid\n#comment=ignored\nvalue2=alsoValid\n[another]\n";
            string sectionName = "TEST";

            bool result = IniParser.TryGetSectionData(contents, sectionName, out Dictionary<string, string> data);

            Assert.True(result, "Empty section should return true");
            Assert.Equal(2, data.Count);
            Assert.Contains<string>("value", data.Keys);
            Assert.Contains<string>("value2", data.Keys);
            Assert.Contains<string>("valid", data.Values);
            Assert.Contains<string>("alsoValid", data.Values);
        }
    }
}
