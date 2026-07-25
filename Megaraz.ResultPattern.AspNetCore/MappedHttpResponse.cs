namespace Megaraz.ResultPattern.AspNetCore
{
    /// <summary>
    /// Framework-agnostic HTTP response descriptor produced by mapping a domain <see cref="Result"/>.
    /// </summary>
    public sealed record MappedHttpResponse(int StatusCode, object? Body = null, string? Location = null);

    /// <summary>
    /// Error response body containing a message and error code.
    /// </summary>
    public sealed record ErrorResponseBody(string Message, string Code);

    /// <summary>
    /// A single field-level validation error for client consumption.
    /// <para>
    /// <c>Field</c> is the field name for UI binding (e.g. <c>"Password"</c>); <c>null</c> means
    /// the error applies to the whole form. <c>Message</c> is the safe, human-readable text
    /// sourced from <see cref="Error.UserMessage"/>.
    /// </para>
    /// <para>
    /// Error codes are intentionally excluded — they remain available on
    /// <see cref="Error.Code"/> for logging and diagnostics but must not be sent to clients.
    /// </para>
    /// </summary>
    public sealed record ValidationErrorItem(string? Field, string Message);

    /// <summary>
    /// Validation error response body containing a top-level message and a list of per-field
    /// <see cref="ValidationErrorItem"/> objects for direct UI binding.
    /// </summary>
    public sealed record ValidationErrorResponseBody(string Message, IEnumerable<ValidationErrorItem>? ValidationErrors);
}
