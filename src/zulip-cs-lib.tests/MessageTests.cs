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
    /// <summary>A send message tests.</summary>
    public class MessageTests
    {
        [Fact]
        public async void Message_SendPrivate_EmailSingle()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            ZulipResponse mockResponse = new ZulipResponse()
            {
                Result = "success",
                Message = "",
                Id = 1,
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            ZulipResponse actual = await zulipClient.SendPrivateMessage("message", "user1@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.Equal(mockResponse.Id, actual.Id);
            Assert.Equal(mockResponse.Result, actual.Result);
        }

        [Fact]
        public async void Message_SendPrivate_EmailMultiple()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            ZulipResponse mockResponse = new ZulipResponse()
            {
                Result = "success",
                Message = "",
                Id = 1,
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            ZulipResponse actual = await zulipClient.SendPrivateMessage("message", "user1@example.org", "user2@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.Equal(mockResponse.Id, actual.Id);
            Assert.Equal(mockResponse.Result, actual.Result);
        }

        [Fact]
        public async void Message_SendPrivate_IdSingle()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            ZulipResponse mockResponse = new ZulipResponse()
            {
                Result = "success",
                Message = "",
                Id = 1,
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            ZulipResponse actual = await zulipClient.SendPrivateMessage("message", 1);

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.Equal(mockResponse.Id, actual.Id);
            Assert.Equal(mockResponse.Result, actual.Result);
        }

        [Fact]
        public async void Message_SendPrivate_IdMultiple()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            ZulipResponse mockResponse = new ZulipResponse()
            {
                Result = "success",
                Message = "",
                Id = 1,
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            ZulipResponse actual = await zulipClient.SendPrivateMessage("message", 1, 2);

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.Equal(mockResponse.Id, actual.Id);
            Assert.Equal(mockResponse.Result, actual.Result);
        }

        [Fact]
        public async void Message_SendPrivate_InvalidEmail()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            ZulipResponse mockResponse = new ZulipResponse()
            {
                Result = "error",
                Message = "Invalid email 'invalid@example.org'",
                ErrorCode = "BAD_REQUEST",
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            ZulipResponse actual = await zulipClient.SendPrivateMessage("message", "invalid@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.Equal(mockResponse.Result, actual.Result);
            Assert.Equal(mockResponse.Message, actual.Message);
            Assert.Equal(mockResponse.ErrorCode, actual.ErrorCode);
        }
    }
}
