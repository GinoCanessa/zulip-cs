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

        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async () => new HttpResponseMessage(code)
            {
                Content = await SnapshotContent(content),
            });

        mockHttpClient = new HttpClient(mockMessageHandler.Object);

        return true;
    }

    /// <summary>Gets a mock HTTP client that captures the request.</summary>
    /// <param name="code">The status code.</param>
    /// <param name="content">The response content.</param>
    /// <param name="capturedRequest">[out] An accessor for the request after sending.</param>
    /// <param name="mockMessageHandler">[out] The mock message handler.</param>
    /// <param name="mockHttpClient">[out] The mock HTTP client.</param>
    /// <returns>True if it succeeds.</returns>
    internal static bool GetMockHttpClientCapture(
        HttpStatusCode code,
        HttpContent content,
        out Func<HttpRequestMessage?> capturedRequest,
        out Mock<HttpMessageHandler> mockMessageHandler,
        out HttpClient mockHttpClient)
    {
        mockMessageHandler = new Mock<HttpMessageHandler>();
        HttpRequestMessage? captured = null;

        mockMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                captured = await SnapshotRequest(request);
                return new HttpResponseMessage(code) { Content = await SnapshotContent(content) };
            });

        mockHttpClient = new HttpClient(mockMessageHandler.Object);
        capturedRequest = () => captured;

        return true;
    }

    /// <summary>Creates a mocked client that records requests and consumes one response per call.</summary>
    /// <param name="requests">The requests observed by the handler.</param>
    /// <param name="responseBodies">The response JSON bodies, in request order.</param>
    /// <returns>The mocked Zulip client.</returns>
    internal static ZulipClient CreateCapturingClient(List<HttpRequestMessage> requests, params string[] responseBodies)
    {
        Queue<string> responses = new Queue<string>(responseBodies);
        Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                requests.Add(await SnapshotRequest(request));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = ContentForJsonString(responses.Dequeue())
                };
            });
        return new ZulipClient(_site, _email, _key, new HttpClient(handler.Object));
    }

    /// <summary>Copies request data while the production request is still alive.</summary>
    /// <param name="request">The request being sent.</param>
    /// <returns>An independent request snapshot.</returns>
    internal static async Task<HttpRequestMessage> SnapshotRequest(HttpRequestMessage request)
    {
        HttpRequestMessage snapshot = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
            Content = request.Content == null ? null : await SnapshotContent(request.Content),
        };
        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            snapshot.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return snapshot;
    }

    /// <summary>Copies content bytes and headers without transferring ownership of the source.</summary>
    /// <param name="content">The source content.</param>
    /// <returns>Independent content for a snapshot or fresh response.</returns>
    private static async Task<HttpContent> SnapshotContent(HttpContent content)
    {
        ByteArrayContent snapshot = new ByteArrayContent(await content.ReadAsByteArrayAsync());
        foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
        {
            snapshot.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return snapshot;
    }

    /// <summary>Reads the decoded form fields from a captured request.</summary>
    /// <param name="request">The captured HTTP request.</param>
    /// <returns>The logical form values, without transport escaping.</returns>
    internal static async Task<Dictionary<string, string>> ReadForm(HttpRequestMessage request)
    {
        return DecodeForm(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync());
    }

    /// <summary>Decodes a query or application/x-www-form-urlencoded body.</summary>
    /// <param name="encoded">The encoded key/value pairs.</param>
    /// <returns>The decoded fields.</returns>
    internal static Dictionary<string, string> DecodeForm(string encoded)
    {
        Dictionary<string, string> fields = [];
        foreach (string pair in encoded.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            fields.Add(Uri.UnescapeDataString(parts[0].Replace("+", " ")),
                Uri.UnescapeDataString(parts.Length == 2 ? parts[1].Replace("+", " ") : string.Empty));
        }
        return fields;
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
