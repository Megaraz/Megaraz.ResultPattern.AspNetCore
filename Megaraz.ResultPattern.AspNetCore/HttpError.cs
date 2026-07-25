using System.Net;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>Classifies an error produced while calling an HTTP service.</summary>
public enum HttpErrorType
{
    /// <summary>A caller-defined HTTP error.</summary>
    Custom = 0,
    /// <summary>The request was invalid.</summary>
    BadRequest = 1,
    /// <summary>Authentication is required.</summary>
    Unauthorized = 2,
    /// <summary>The caller is not permitted to perform the request.</summary>
    Forbidden = 3,
    /// <summary>The requested resource was not found.</summary>
    NotFound = 4,
    /// <summary>The request conflicts with the current state.</summary>
    Conflict = 5,
    /// <summary>The external service failed internally.</summary>
    InternalServerError = 6,
    /// <summary>The external service could not process the request.</summary>
    UnprocessableContent = 7,
    /// <summary>The request was rate limited.</summary>
    TooManyRequests = 8,
    /// <summary>The external service could not be reached.</summary>
    TransportFailure = 9,
    /// <summary>The response body was invalid or unsupported.</summary>
    MalformedResponse = 10,
    /// <summary>The response status was not recognized.</summary>
    UnexpectedStatusCode = 11
}

/// <summary>
/// Represents an HTTP-specific external error while retaining the core package's
/// <see cref="ErrorType.External"/> classification.
/// </summary>
public sealed record HttpError : Error
{
    private HttpError(
        string code,
        string description,
        HttpErrorType type,
        string userMessage,
        Exception? exception = null)
        : base(code, description, ErrorType.External, userMessage, exception)
    {
        HttpErrorType = type;
    }

    /// <summary>Gets the HTTP-specific classification.</summary>
    public HttpErrorType HttpErrorType { get; }

    internal static HttpError FromResponse(
        ErrorContext errorContext,
        HttpStatusCode statusCode,
        string? description,
        string? userMessage)
    {
        var details = string.IsNullOrWhiteSpace(description) ? null : description;

        return statusCode switch
        {
            HttpStatusCode.NotFound => CreateWithDetails(errorContext, "NotFound", HttpErrorType.NotFound,
                details ?? "The external service returned not found.", "Not Found", userMessage),
            HttpStatusCode.BadRequest => CreateWithDetails(errorContext, "BadRequest", HttpErrorType.BadRequest,
                details ?? "The external service rejected the request.", "Bad Request", userMessage),
            HttpStatusCode.UnprocessableContent => CreateWithDetails(errorContext, "UnprocessableContent",
                HttpErrorType.UnprocessableContent, details ?? "The external service could not process the request.",
                "Unprocessable Content", userMessage),
            HttpStatusCode.Conflict => CreateWithDetails(errorContext, "Conflict", HttpErrorType.Conflict,
                details ?? "The external service reported a conflict.", "Conflict", userMessage),
            HttpStatusCode.Unauthorized => CreateWithDetails(errorContext, "Unauthorized", HttpErrorType.Unauthorized,
                details ?? "The external service requires authentication.", "Unauthorized", userMessage),
            HttpStatusCode.Forbidden => CreateWithDetails(errorContext, "Forbidden", HttpErrorType.Forbidden,
                details ?? "The external service refused the request.", "Forbidden", userMessage),
            HttpStatusCode.InternalServerError => CreateWithDetails(errorContext, "InternalServerError",
                HttpErrorType.InternalServerError, details ?? "The external service encountered an internal server error.",
                "Internal Server Error", userMessage),
            HttpStatusCode.TooManyRequests => CreateWithDetails(errorContext, "TooManyRequests",
                HttpErrorType.TooManyRequests, details ?? "The external service has rate-limited this request.",
                "Too Many Requests", userMessage),
            _ => CreateWithDetails(
                errorContext,
                "UnexpectedStatusCode",
                HttpErrorType.UnexpectedStatusCode,
                details ?? $"The external service returned an unexpected HTTP status code {(int)statusCode} ({statusCode}).",
                "Unexpected external service response",
                userMessage)
        };
    }

    /// <summary>Creates a caller-defined HTTP error.</summary>
    public static HttpError Custom(ErrorContext errorContext, string customDescriptionSuffix) =>
        Create(errorContext, "Custom", HttpErrorType.Custom, customDescriptionSuffix, customDescriptionSuffix);

    /// <summary>Creates an error for a transport failure.</summary>
    public static HttpError TransportFailure(ErrorContext errorContext, Exception? exception = null) =>
        Create(errorContext, "TransportFailure", HttpErrorType.TransportFailure,
            "The external service could not be reached.", "Transport failure", exception);

    /// <summary>Creates an error for an HTTP 429 response.</summary>
    public static HttpError TooManyRequests(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "TooManyRequests", HttpErrorType.TooManyRequests,
            "Too Many Requests", callerMessage);

    /// <summary>Creates an error for an invalid response body.</summary>
    public static HttpError MalformedResponse(
        ErrorContext errorContext,
        Exception? exception = null,
        string? detail = null) =>
        Create(errorContext, "MalformedResponse", HttpErrorType.MalformedResponse,
            detail ?? "The external service returned a malformed or unexpected response.",
            "The external service returned a malformed or unexpected response.", exception);

    /// <summary>Creates an error for an unexpected HTTP status code.</summary>
    public static HttpError UnexpectedStatusCode(ErrorContext errorContext, HttpStatusCode statusCode)
    {
        var message = $"The external service returned an unexpected HTTP status code {(int)statusCode} ({statusCode}).";
        return Create(errorContext, "UnexpectedStatusCode", HttpErrorType.UnexpectedStatusCode, message, message);
    }

    /// <summary>Creates an error for an HTTP 422 response.</summary>
    public static HttpError UnprocessableContent(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "UnprocessableContent", HttpErrorType.UnprocessableContent,
            "Unprocessable Content", callerMessage);

    /// <summary>Creates an error for an HTTP 400 response.</summary>
    public static HttpError BadRequest(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "BadRequest", HttpErrorType.BadRequest, "Bad Request", callerMessage);

    /// <summary>Creates an error for an HTTP 401 response.</summary>
    public static HttpError UnauthorizedAccess(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "Unauthorized", HttpErrorType.Unauthorized, "Unauthorized", callerMessage);

    /// <summary>Creates an error for an HTTP 403 response.</summary>
    public static HttpError Forbidden(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "Forbidden", HttpErrorType.Forbidden, "Forbidden", callerMessage);

    /// <summary>Creates an error for an HTTP 404 response.</summary>
    public static HttpError NotFound(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "NotFound", HttpErrorType.NotFound, "Not Found", callerMessage);

    /// <summary>Creates an error for an HTTP 409 response.</summary>
    public static HttpError Conflict(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "Conflict", HttpErrorType.Conflict, "Conflict", callerMessage);

    /// <summary>Creates an error for an HTTP 500 response.</summary>
    public static HttpError InternalServerError(ErrorContext errorContext, string? callerMessage = null) =>
        CreateWithDefaultMessage(errorContext, "InternalServerError", HttpErrorType.InternalServerError,
            "Internal Server Error", callerMessage);

    private static HttpError CreateWithDefaultMessage(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string defaultMessage,
        string? callerMessage)
    {
        var message = string.IsNullOrWhiteSpace(callerMessage) ? defaultMessage : callerMessage;
        return Create(context, reason, type, message, message);
    }

    private static HttpError CreateWithDetails(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string description,
        string defaultUserMessage,
        string? userMessage)
    {
        return Create(context, reason, type, description, userMessage ?? defaultUserMessage);
    }

    private static HttpError Create(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string description,
        string userMessage,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var code = ErrorCode.For(context, reason);
        var formattedDescription = $"{context.EntityName}: {description}";
        return new HttpError(code.Code, formattedDescription, type, userMessage, exception);
    }
}
