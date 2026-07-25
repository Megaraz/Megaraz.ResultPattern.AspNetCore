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
                details ?? "The external service returned not found.", userMessage),
            HttpStatusCode.BadRequest => CreateWithDetails(errorContext, "BadRequest", HttpErrorType.BadRequest,
                details ?? "The external service rejected the request.", userMessage),
            HttpStatusCode.UnprocessableContent => CreateWithDetails(errorContext, "UnprocessableContent",
                HttpErrorType.UnprocessableContent, details ?? "The external service could not process the request.",
                userMessage),
            HttpStatusCode.Conflict => CreateWithDetails(errorContext, "Conflict", HttpErrorType.Conflict,
                details ?? "The external service reported a conflict.", userMessage),
            HttpStatusCode.Unauthorized => CreateWithDetails(errorContext, "Unauthorized", HttpErrorType.Unauthorized,
                details ?? "The external service requires authentication.", userMessage),
            HttpStatusCode.Forbidden => CreateWithDetails(errorContext, "Forbidden", HttpErrorType.Forbidden,
                details ?? "The external service refused the request.", userMessage),
            HttpStatusCode.InternalServerError => CreateWithDetails(errorContext, "InternalServerError",
                HttpErrorType.InternalServerError, details ?? "The external service encountered an internal server error.",
                userMessage),
            HttpStatusCode.TooManyRequests => CreateWithDetails(errorContext, "TooManyRequests",
                HttpErrorType.TooManyRequests, details ?? "The external service has rate-limited this request.",
                userMessage),
            _ => CreateWithDetails(
                errorContext,
                "UnexpectedStatusCode",
                HttpErrorType.UnexpectedStatusCode,
                details ?? $"The external service returned an unexpected HTTP status code {(int)statusCode} ({statusCode}).",
                userMessage)
        };
    }

    /// <summary>Creates a caller-defined HTTP error.</summary>
    public static HttpError Custom(
        ErrorContext errorContext,
        string customDescriptionSuffix,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null,
        Exception? exception = null) =>
        Create(errorContext, "Custom", HttpErrorType.Custom, customDescriptionSuffix, userMessage,
            messageFactory, exception);

    /// <summary>Creates an error for a transport failure.</summary>
    public static HttpError TransportFailure(
        ErrorContext errorContext,
        Exception? exception = null,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        Create(errorContext, "TransportFailure", HttpErrorType.TransportFailure,
            "The external service could not be reached.", userMessage, messageFactory, exception);

    /// <summary>Creates an error for an HTTP 429 response.</summary>
    public static HttpError TooManyRequests(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "TooManyRequests", HttpErrorType.TooManyRequests,
            "Too Many Requests", userMessage, messageFactory);

    /// <summary>Creates an error for an invalid response body.</summary>
    public static HttpError MalformedResponse(
        ErrorContext errorContext,
        Exception? exception = null,
        string? detail = null,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        Create(errorContext, "MalformedResponse", HttpErrorType.MalformedResponse,
            detail ?? "The external service returned a malformed or unexpected response.",
            userMessage, messageFactory, exception);

    /// <summary>Creates an error for an unexpected HTTP status code.</summary>
    public static HttpError UnexpectedStatusCode(
        ErrorContext errorContext,
        HttpStatusCode statusCode,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null)
    {
        var message = $"The external service returned an unexpected HTTP status code {(int)statusCode} ({statusCode}).";
        return Create(errorContext, "UnexpectedStatusCode", HttpErrorType.UnexpectedStatusCode, message,
            userMessage, messageFactory);
    }

    /// <summary>Creates an error for an HTTP 422 response.</summary>
    public static HttpError UnprocessableContent(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "UnprocessableContent", HttpErrorType.UnprocessableContent,
            "Unprocessable Content", userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 400 response.</summary>
    public static HttpError BadRequest(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "BadRequest", HttpErrorType.BadRequest, "Bad Request",
            userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 401 response.</summary>
    public static HttpError UnauthorizedAccess(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "Unauthorized", HttpErrorType.Unauthorized, "Unauthorized",
            userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 403 response.</summary>
    public static new HttpError Forbidden(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "Forbidden", HttpErrorType.Forbidden, "Forbidden",
            userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 404 response.</summary>
    public static new HttpError NotFound(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "NotFound", HttpErrorType.NotFound, "Not Found",
            userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 409 response.</summary>
    public static new HttpError Conflict(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "Conflict", HttpErrorType.Conflict, "Conflict",
            userMessage, messageFactory);

    /// <summary>Creates an error for an HTTP 500 response.</summary>
    public static HttpError InternalServerError(
        ErrorContext errorContext,
        string? userMessage = null,
        Func<ErrorContext, string>? messageFactory = null) =>
        CreateWithDefaultMessage(errorContext, "InternalServerError", HttpErrorType.InternalServerError,
            "Internal Server Error", userMessage, messageFactory);

    private static HttpError CreateWithDefaultMessage(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string defaultMessage,
        string? userMessage,
        Func<ErrorContext, string>? messageFactory)
    {
        return Create(context, reason, type, defaultMessage, userMessage, messageFactory);
    }

    private static HttpError CreateWithDetails(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string description,
        string? userMessage)
    {
        return Create(context, reason, type, description, userMessage ?? string.Empty);
    }

    private static HttpError Create(
        ErrorContext context,
        string reason,
        HttpErrorType type,
        string description,
        string? userMessage,
        Func<ErrorContext, string>? messageFactory = null,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var code = ErrorCode.For(context, reason);
        var formattedDescription = $"{context.EntityName}: {description}";
        var resolvedDescription = messageFactory is null ? formattedDescription : messageFactory(context);
        if (string.IsNullOrWhiteSpace(resolvedDescription))
            throw new ArgumentException("The message factory must return a non-blank message.", nameof(messageFactory));
        return new HttpError(code.Code, resolvedDescription, type, userMessage ?? string.Empty, exception);
    }
}
