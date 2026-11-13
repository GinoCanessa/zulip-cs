using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib
{
    /// <summary>The outcome of one server batch, without automatic continuation.</summary>
    public sealed class BatchOperationResult
    {
        /// <summary>Gets completion: true for complete, false for partial, or null for an unspecified legacy result.</summary>
        public bool? Complete { get; }

        /// <summary>Gets the request-local response, including warnings and diagnostics.</summary>
        public ZulipResponse Response { get; }

        /// <summary>Initializes a batch outcome.</summary>
        /// <param name="complete">The server's completion state.</param>
        /// <param name="response">The request-local response.</param>
        private BatchOperationResult(bool? complete, ZulipResponse response)
        {
            Complete = complete;
            Response = response;
        }

        /// <summary>Reads modern and legacy partial-completion responses.</summary>
        /// <param name="response">The server response.</param>
        /// <param name="operation">The operation name for error details.</param>
        /// <returns>The batch outcome or an explicit error.</returns>
        internal static (bool success, string? details, BatchOperationResult? batch) Read(
            ZulipResponse response, string operation)
        {
            if (response.Result == "partially_completed" && string.IsNullOrEmpty(response.CaughtException))
            {
                return (true, null, new BatchOperationResult(false, response));
            }

            if (!ResponsePayload.TryRead<CompletionPayload>(
                response, operation, null, JsonValueKind.Object,
                out CompletionPayload? payload, out string? details, ValidateCompletion))
            {
                return (false, details, null);
            }

            return (true, payload!.Complete == null
                ? operation + ": Legacy response did not specify completion; inspect the batch result before assuming all work is complete."
                : null, new BatchOperationResult(payload.Complete, response));
        }

        /// <summary>Projects the batch onto the legacy completion-oriented API.</summary>
        /// <param name="operation">The operation name.</param>
        /// <param name="batchMethod">The detail method callers can use for continuation.</param>
        /// <returns>False for known partial work; legacy unknown completion remains explicit.</returns>
        internal (bool success, string? details) ToCompletionResult(string operation, string batchMethod)
        {
            return Complete switch
            {
                false => (false, operation + ": Work is incomplete; use " + batchMethod + " to inspect and continue batches."),
                null => (true, operation + ": Legacy response did not specify completion; use " + batchMethod + " for explicit completion state."),
                true => (true, null),
            };
        }

        /// <summary>Rejects a present completion field that is not a boolean.</summary>
        /// <param name="root">The response object.</param>
        /// <returns>An error or null.</returns>
        private static string? ValidateCompletion(JsonElement root)
        {
            return root.TryGetProperty("complete", out JsonElement complete) &&
                complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                ? "field 'complete' must be a boolean when present."
                : null;
        }

        /// <summary>The endpoint-specific completion payload.</summary>
        internal sealed class CompletionPayload
        {
            /// <summary>Gets or sets the optional completion field.</summary>
            [JsonPropertyName("complete")]
            public bool? Complete { get; set; }
        }
    }
}
