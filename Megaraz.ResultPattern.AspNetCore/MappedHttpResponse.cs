namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Framework-agnostic HTTP response descriptor produced by mapping a domain <see cref="Result"/>.
/// </summary>
/// <remarks>
/// Successful responses contain the application value in <see cref="Body"/>. Failed responses
/// use either <see cref="ErrorResponseBody"/> or <see cref="ValidationErrorResponseBody"/> by
/// default. The body remains <see cref="object"/> to allow application-specific success DTOs
/// and custom failure-body factories.
/// </remarks>
public sealed record MappedHttpResponse(int StatusCode, object? Body = null, string? Location = null);

/// <summary>
/// The stable default body for a non-validation failure.
/// </summary>
/// <remarks>
/// The <c>code</c> is an application-facing, machine-readable identifier. Applications should
/// treat codes as versioned API contract values and change them only deliberately.
/// </remarks>
public sealed record ErrorResponseBody(string Message, string Code);

/// <summary>
/// A single field-level validation error for client consumption.
/// </summary>
/// <param name="Field">
/// The field name for UI binding; <see langword="null"/> means the error applies to the whole form.
/// </param>
/// <param name="Message">Safe, human-readable text sourced from <see cref="Error.UserMessage"/>.</param>
public sealed record ValidationErrorItem(string? Field, string Message);

/// <summary>
/// The stable default body for a validation failure.
/// </summary>
/// <remarks>
/// Validation uses the same public <c>code</c> policy as other failures. The list is copied when
/// this DTO is created, so deferred or later-mutated input cannot change the response contract.
/// </remarks>
public sealed record ValidationErrorResponseBody
{
    /// <summary>Initializes a validation error response body.</summary>
    public ValidationErrorResponseBody(
        string message,
        string code,
        IReadOnlyList<ValidationErrorItem> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(validationErrors);

        Message = message;
        Code = code;
        ValidationErrors = validationErrors.ToArray();
    }

    /// <summary>Gets the top-level safe message.</summary>
    public string Message { get; }

    /// <summary>Gets the stable machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the materialized validation errors.</summary>
    public IReadOnlyList<ValidationErrorItem> ValidationErrors { get; }
}
