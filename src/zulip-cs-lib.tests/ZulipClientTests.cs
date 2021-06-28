using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;


namespace zulip_set_lib.tests
{
    /// <summary>Basic Zulip client tests.</summary>
    public class ZulipClientTests
    {
        private const string _email = "zulip@example.org";
        private const string _key = "KeyInBase64";
        private const string _site = "https://zulip.example.org";


        [Fact]
        public void ZulipClient_CreateMockHttp()
        {
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", "mocked message" },
            };

            bool success = Utils.GetMockHttpClient(
                HttpStatusCode.OK,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> httpHandler,
                out HttpClient httpClient);

            Assert.True(success, "Failed to create mock HttpClient");
        }

        [Fact]
        public void ZulipClient_Create_Success()
        {
            ZulipClient client = new ZulipClient(_site, _email, _key);
        }

        [Fact]
        public void ZulipClient_GetMockedClient()
        {
            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK,
                new StringContent("OK"),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get a valid mocked client!");
        }

        [Fact]
        public void ZulipClient_CreateSiteEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(string.Empty, _email, _key));
            Assert.Equal("site", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateSiteInvalid_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient("uri://invalid.me", _email, _key));
            Assert.Equal("site", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateEmailEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, string.Empty, _key));
            Assert.Equal("userEmail", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateEmailInvalid_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, "NotAnEmail", _key));
            Assert.Equal("userEmail", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateKeyEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, _email, string.Empty));
            Assert.Equal("apiKey", ex.Message);
        }

    }
}
