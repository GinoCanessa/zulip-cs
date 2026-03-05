using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;


namespace zulip_set_lib.tests;

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

    /// <summary>Gets a mock HTTP client that captures the request.</summary>
    /// <param name="code">The status code.</param>
    /// <param name="content">The response content.</param>
    /// <param name="capturedRequest">[out] The captured request.</param>
    /// <param name="mockMessageHandler">[out] The mock message handler.</param>
    /// <param name="mockHttpClient">[out] The mock HTTP client.</param>
    /// <returns>True if it succeeds.</returns>
    internal static bool GetMockHttpClientCapture(
        HttpStatusCode code,
        HttpContent content,
        out HttpRequestMessage capturedRequest,
        out Mock<HttpMessageHandler> mockMessageHandler,
        out HttpClient mockHttpClient)
    {
        mockMessageHandler = new Mock<HttpMessageHandler>();
        HttpRequestMessage captured = null;

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
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(mockResponse);

        mockHttpClient = new HttpClient(mockMessageHandler.Object);
        capturedRequest = captured;

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

    /// <summary>Creates HttpContent from a raw JSON string.</summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>A HttpContent.</returns>
    internal static HttpContent ContentForJsonString(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

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
                    WriteValue(writer, kvp.Key, kvp.Value);
                }

                writer.WriteEndObject();
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (StreamReader reader = new StreamReader(stream))
            {
                return new StringContent(
                    reader.ReadToEnd(),
                    Encoding.UTF8,
                    "application/json");
            }
        }
    }

    /// <summary>Writes a value to a JSON writer, supporting nested types.</summary>
    private static void WriteValue(Utf8JsonWriter writer, string key, dynamic value)
    {
        switch (value)
        {
            case int valueInt:
                writer.WriteNumber(key, valueInt);
                break;

            case long valueLong:
                writer.WriteNumber(key, valueLong);
                break;

            case ulong valueUlong:
                writer.WriteNumber(key, valueUlong);
                break;

            case string valueString:
                writer.WriteString(key, valueString);
                break;

            case bool valueBool:
                writer.WriteBoolean(key, valueBool);
                break;

            case null:
                writer.WriteNull(key);
                break;

            default:
                // For anything else, serialize as raw JSON
                writer.WritePropertyName(key);
                JsonSerializer.Serialize(writer, value);
                break;
        }
    }
}
