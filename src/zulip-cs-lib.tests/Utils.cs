using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;


namespace zulip_set_lib.tests
{
    internal static class Utils
    {
        private const string _email = "zulip@example.org";
        private const string _key = "KeyInBase64";
        private const string _site = "https://zulip.example.org";


        /// <summary>Gets mock HTTP client.</summary>
        /// <param name="code">              The code.</param>
        /// <param name="content">           The content.</param>
        /// <param name="mockMessageHandler">[out] The mock message handler.</param>
        /// <param name="mockHttpClient">    [out] The mock HTTP client.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        internal static bool GetMockHttpClient(
            HttpStatusCode code,
            HttpContent content,
            out Mock<HttpMessageHandler> mockMessageHandler,
            out HttpClient mockHttpClient)
        {
            mockMessageHandler = new Mock<HttpMessageHandler>();

            HttpResponseMessage mockResponse = new HttpResponseMessage()
            {
                StatusCode = code,
                Content = content,
            };

            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            mockHttpClient = new HttpClient(mockMessageHandler.Object);

            return true;
        }

        /// <summary>Attempts to get mocked client.</summary>
        /// <param name="code">              The code.</param>
        /// <param name="content">           The content.</param>
        /// <param name="mockMessageHandler">[out] The mock message handler.</param>
        /// <param name="mockHttpClient">    [out] The mock HTTP client.</param>
        /// <param name="zulipClient">       [out] The zulip client.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        internal static bool TryGetMockedClient(
            HttpStatusCode code,
            HttpContent content,
            out Mock<HttpMessageHandler> mockMessageHandler,
            out HttpClient mockHttpClient,
            out ZulipClient zulipClient)
        {
            bool success = GetMockHttpClient(
                code,
                content,
                out mockMessageHandler,
                out mockHttpClient);

            zulipClient = new ZulipClient(_site, _email, _key, mockHttpClient);

            return true;
        }

        ///// <summary>Content for response.</summary>
        ///// <param name="response">The response.</param>
        ///// <returns>A HttpContent.</returns>
        //internal static HttpContent ContentForResponse(ZulipResponse response)
        //{
        //    return new StringContent(
        //        System.Text.Json.JsonSerializer.Serialize(response),
        //        System.Text.Encoding.UTF8,
        //        "application/json");
        //}

        /// <summary>Content for response.</summary>
        /// <param name="response">The response.</param>
        /// <returns>A HttpContent.</returns>
        internal static HttpContent ContentForResponse(Dictionary<string, dynamic> response)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();

                    foreach (KeyValuePair<string, dynamic> kvp in response)
                    {
                        switch (kvp.Value)
                        {
                            case int valueInt:
                                writer.WriteNumber(kvp.Key, valueInt);
                                break;

                            case ulong valueUlong:
                                writer.WriteNumber(kvp.Key, valueUlong);
                                break;

                            case string valueString:
                                writer.WriteString(kvp.Key, valueString);
                                break;
                        }
                    }

                    writer.WriteEndObject();
                }

                stream.Seek(0, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(stream))
                {
                    return new StringContent(
                        reader.ReadToEnd(),
                        System.Text.Encoding.UTF8,
                        "application/json");
                }
            }
        }
    }
}
