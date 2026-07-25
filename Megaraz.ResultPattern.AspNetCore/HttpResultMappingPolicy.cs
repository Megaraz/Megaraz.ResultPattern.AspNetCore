using Megaraz.ResultPattern;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Defines the application-owned policy used when mapping results to HTTP responses.
/// </summary>
/// <remarks>
/// Use <see cref="Default"/> for the standard mappings and a <c>with</c> expression to
/// override only the rules an application needs to change.
/// </remarks>
public sealed record HttpResultMappingPolicy
{
    /// <summary>Gets the default mapping policy.</summary>
    public static HttpResultMappingPolicy Default { get; } = new();

    /// <summary>Gets the status code used for a successful response with a body.</summary>
    public int SuccessStatusCode { get; init; } = 200;

    /// <summary>Gets the status code used for a successful response without a body.</summary>
    public int NoContentStatusCode { get; init; } = 204;

    /// <summary>Gets the status code used for a successful create response.</summary>
    public int CreatedStatusCode { get; init; } = 201;

    /// <summary>Gets the status-code selector for non-HTTP core error types.</summary>
    public Func<ErrorType, int> ErrorTypeStatusCode { get; init; } = GetDefaultErrorTypeStatusCode;

    /// <summary>Gets the status-code selector for <see cref="HttpErrorType"/> values.</summary>
    public Func<HttpErrorType, int> HttpErrorTypeStatusCode { get; init; } = GetDefaultHttpErrorTypeStatusCode;

    /// <summary>
    /// Gets the factory that creates the body for a failed result.
    /// </summary>
    public Func<Result, object> FailureBodyFactory { get; init; } = HttpResultMapper.CreateDefaultFailureBody;

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(ErrorTypeStatusCode);
        ArgumentNullException.ThrowIfNull(HttpErrorTypeStatusCode);
        ArgumentNullException.ThrowIfNull(FailureBodyFactory);
    }

    private static int GetDefaultErrorTypeStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => 422,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            ErrorType.Failure => 500,
            ErrorType.Cancelled => 503,
            ErrorType.External => 502,
            _ => 400
        };

    private static int GetDefaultHttpErrorTypeStatusCode(HttpErrorType errorType) =>
        errorType switch
        {
            HttpErrorType.BadRequest => 400,
            HttpErrorType.Unauthorized => 401,
            HttpErrorType.Forbidden => 403,
            HttpErrorType.NotFound => 404,
            HttpErrorType.Conflict => 409,
            HttpErrorType.UnprocessableContent => 422,
            HttpErrorType.TooManyRequests => 429,
            HttpErrorType.TransportFailure => 503,
            _ => 502
        };
}
