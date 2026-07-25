using System.Net;
using System.Text.Json;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>Converts HTTP responses to core result values.</summary>
public static class HttpResponseToResultExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Maps a successful JSON response or an HTTP failure to a typed result.</summary>
    public static async Task<Result<TValue>> MapToResultAsync<TValue>(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        CancellationToken ct = default)
        where TValue : notnull
    {
        return await MapToResultAsync<TValue>(response, errorContext, HttpResponseMappingOptions.Default, ct);
    }

    /// <summary>Maps a response using explicit application-owned upstream message options.</summary>
    public static async Task<Result<TValue>> MapToResultAsync<TValue>(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        HttpResponseMappingOptions options,
        CancellationToken ct = default)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(options);

        if (response is null)
            return Result<TValue>.Failure(HttpError.TransportFailure(errorContext));

        if (!response.IsSuccessStatusCode)
        {
            var failureMessage = await GetFailureMessageAsync(response, ct);
            return CreateHttpFailureResult<TValue>(response.StatusCode, errorContext, failureMessage, options);
        }

        var responseBody = await ReadResponseBodyAsync(response, ct);
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(
                errorContext,
                detail: $"The external service returned {(int)response.StatusCode}" +
                $" ({response.StatusCode}) without the expected response body."));
        }

        if (!HasJsonContentType(response))
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(
                errorContext,
                detail: $"The external service returned {(int)response.StatusCode}" +
                $" ({response.StatusCode}) with unsupported content type '{response.Content.Headers.ContentType?.MediaType ?? "unknown"}'."));
        }

        try
        {
            var value = JsonSerializer.Deserialize<TValue>(responseBody, JsonSerializerOptions);

            if (value is null)
            {
                return Result<TValue>.Failure(HttpError.MalformedResponse(
                    errorContext,
                    detail: $"The external service returned {(int)response.StatusCode}" +
                    $" ({response.StatusCode}) with an empty or invalid JSON body."));
            }

            return Result<TValue>.Success(value);
        }
        catch (JsonException exception)
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(errorContext, exception, "The external service returned malformed JSON."));
        }
        catch (NotSupportedException exception)
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(errorContext, exception, "The external service returned an unsupported JSON payload."));
        }
    }

    /// <summary>Maps a successful response or an HTTP failure to a non-generic result.</summary>
    public static async Task<Result> MapToResultAsync(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        CancellationToken ct = default)
    {
        return await MapToResultAsync(response, errorContext, HttpResponseMappingOptions.Default, ct);
    }

    /// <summary>Maps a response using explicit application-owned upstream message options.</summary>
    public static async Task<Result> MapToResultAsync(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        HttpResponseMappingOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (response is null)
            return Result.Failure(HttpError.TransportFailure(errorContext));

        if (response.IsSuccessStatusCode)
            return Result.Success();

        var failureMessage = await GetFailureMessageAsync(response, ct);

        return CreateHttpFailureResult(response.StatusCode, errorContext, failureMessage, options);
    }

    private static Result<TValue> CreateHttpFailureResult<TValue>(
        HttpStatusCode statusCode,
        ErrorContext errorContext,
        string responseMessage,
        HttpResponseMappingOptions options)
        where TValue : notnull
    {
        return Result<TValue>.Failure(MapHttpError(statusCode, errorContext, responseMessage, options));
    }

    private static Result CreateHttpFailureResult(
        HttpStatusCode statusCode,
        ErrorContext errorContext,
        string responseMessage,
        HttpResponseMappingOptions options)
    {
        return Result.Failure(MapHttpError(statusCode, errorContext, responseMessage, options));
    }

    private static HttpError MapHttpError(
        HttpStatusCode statusCode,
        ErrorContext errorContext,
        string responseMessage,
        HttpResponseMappingOptions options)
    {
        return HttpError.FromResponse(
            errorContext,
            statusCode,
            responseMessage,
            options.UserMessageFactory?.Invoke(responseMessage));
    }

    private static async Task<string> GetFailureMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var responseMessage = await TryGetResponseMessageAsync(response, ct);
        return BuildFailureMessage(responseMessage, GetDefaultFailureMessage(response.StatusCode), response.ReasonPhrase);
    }

    private static string BuildFailureMessage(string? responseMessage, string defaultMessage, string? reasonPhrase)
    {
        return FirstNonEmpty(responseMessage, defaultMessage, reasonPhrase, "An error occurred while calling the external service.");
    }

    private static string GetDefaultFailureMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "The external service rejected the request.",
            HttpStatusCode.Unauthorized => "The external service requires authentication.",
            HttpStatusCode.Forbidden => "The external service refused the request.",
            HttpStatusCode.NotFound => "The requested resource was not found in the external service.",
            HttpStatusCode.Conflict => "The external service reported a conflict.",
            HttpStatusCode.UnprocessableContent => "The external service could not process the request.",
            HttpStatusCode.InternalServerError => "The external service encountered an internal server error.",
            HttpStatusCode.TooManyRequests => "The external service has rate-limited this request.",
            _ => $"The external service returned an unexpected HTTP status code {(int)statusCode} ({statusCode}).",
        };
    }

    // Returns true when the Content-Type header is absent (null or whitespace) as well as when it
    // contains "json". This leniency is intentional: some real-world external APIs (e.g. RAWG)
    // return valid JSON bodies without setting a Content-Type header on success responses.
    // Callers that need strict enforcement should inspect the header themselves before mapping.
    private static bool HasJsonContentType(HttpResponseMessage response)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType;

        return string.IsNullOrWhiteSpace(mediaType)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content is null)
            return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private static async Task<string?> TryGetResponseMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var responseBody = await ReadResponseBodyAsync(response, ct);
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return responseBody.Trim();

            foreach (var propertyName in new[] { "message", "detail", "title", "error", "error_description" })
            {
                if (document.RootElement.TryGetProperty(propertyName, out var property)
                    && property.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.GetString()))
                {
                    return property.GetString();
                }
            }

            if (document.RootElement.TryGetProperty("errors", out var errorsProperty))
            {
                var errors = ExtractErrors(errorsProperty);
                if (!string.IsNullOrWhiteSpace(errors))
                    return errors;
            }
        }
        catch (JsonException)
        {
        }

        return responseBody.Trim();
    }

    private static string? ExtractErrors(JsonElement errorsElement)
    {
        if (errorsElement.ValueKind == JsonValueKind.Array)
        {
            var errors = errorsElement
                .EnumerateArray()
                .Where(static value => value.ValueKind == JsonValueKind.String)
                .Select(static value => value.GetString())
                .Where(static value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" ", errors!);
        }

        if (errorsElement.ValueKind == JsonValueKind.Object)
        {
            var errors = errorsElement
                .EnumerateObject()
                .SelectMany(static property => property.Value.ValueKind == JsonValueKind.Array
                    ? property.Value.EnumerateArray()
                        .Where(static value => value.ValueKind == JsonValueKind.String)
                        .Select(static value => value.GetString())
                    : Array.Empty<string?>())
                .Where(static value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" ", errors!);
        }

        return null;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "An error occurred while calling the external service.";
    }
}
