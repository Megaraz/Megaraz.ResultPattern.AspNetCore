using System.Text.Json;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Configures how upstream HTTP response details are represented in mapped errors.
/// </summary>
public sealed record HttpResponseMappingOptions
{
    /// <summary>Gets the default options.</summary>
    public static HttpResponseMappingOptions Default { get; } = new();

    /// <summary>
    /// Gets the serializer options used for successful response bodies.
    /// </summary>
    /// <remarks>
    /// When not specified, <see cref="JsonSerializerDefaults.Web"/> options are used.
    /// </remarks>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    /// <summary>
    /// Gets the maximum number of response-body bytes inspected by the default reader.
    /// </summary>
    /// <remarks>
    /// Response content is consumed while mapping. Use a streaming deserializer overload when
    /// the payload must not be buffered.
    /// </remarks>
    public int MaxResponseBodyBytes { get; init; } = 64 * 1024;

    /// <summary>
    /// Gets the optional application-owned callback that converts upstream response text into a
    /// user-facing message.
    /// </summary>
    /// <remarks>
    /// The callback is an explicit opt-in. Validate or replace the supplied text before returning
    /// it because upstream response content is untrusted.
    /// </remarks>
    public Func<string, string?>? UserMessageFactory { get; init; }

    /// <summary>
    /// Gets the optional application-owned callback that extracts a message from an error body.
    /// </summary>
    /// <remarks>
    /// The callback receives the inspected response text. When it returns <see langword="null"/>
    /// or whitespace, the status-specific fallback message is used.
    /// </remarks>
    public Func<string, string?>? ErrorMessageExtractor { get; init; }

    internal void Validate()
    {
        if (MaxResponseBodyBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxResponseBodyBytes), "The response-body limit must be at least 1 byte.");
    }
}
