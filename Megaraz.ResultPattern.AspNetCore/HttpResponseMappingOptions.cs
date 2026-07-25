namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Configures how upstream HTTP response details are represented in mapped errors.
/// </summary>
public sealed record HttpResponseMappingOptions
{
    /// <summary>Gets the default options, which do not expose upstream text as a user message.</summary>
    public static HttpResponseMappingOptions Default { get; } = new();

    /// <summary>
    /// Gets the optional application-owned callback that converts upstream response text into a
    /// user-facing message.
    /// </summary>
    /// <remarks>
    /// The callback is an explicit opt-in. Validate or replace the supplied text before returning
    /// it because upstream response content is untrusted.
    /// </remarks>
    public Func<string, string?>? UserMessageFactory { get; init; }
}
