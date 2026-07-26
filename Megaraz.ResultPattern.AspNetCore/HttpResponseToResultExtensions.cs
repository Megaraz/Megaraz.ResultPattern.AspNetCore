using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>Converts HTTP responses and selected HTTP client exceptions to core result values.</summary>
public static class HttpResponseToResultExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions =
        new(JsonSerializerDefaults.Web);

    /// <summary>Maps a successful JSON response or an HTTP failure to a typed result.</summary>
    /// <param name="response">The response to consume, or <see langword="null"/> for a transport failure.</param>
    /// <param name="errorContext">The operation and entity context for generated errors.</param>
    /// <param name="ct">The token used to cancel response-body consumption.</param>
    /// <returns>A typed result containing the deserialized value or an HTTP error.</returns>
    public static Task<Result<TValue>> MapToResultAsync<TValue>(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        CancellationToken ct = default)
        where TValue : notnull =>
        MapToResultAsync<TValue>(response, errorContext, HttpResponseMappingOptions.Default, ct);

    /// <summary>Maps a response using configured serialization and upstream-message options.</summary>
    /// <param name="response">The response to consume, or <see langword="null"/> for a transport failure.</param>
    /// <param name="errorContext">The operation and entity context for generated errors.</param>
    /// <param name="options">The serialization, body-limit, and message-extraction options.</param>
    /// <param name="ct">The token used to cancel response-body consumption.</param>
    /// <returns>A typed result containing the deserialized value or an HTTP error.</returns>
    public static async Task<Result<TValue>> MapToResultAsync<TValue>(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        HttpResponseMappingOptions options,
        CancellationToken ct = default)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (response is null)
            return Result<TValue>.Failure(HttpError.TransportFailure(errorContext));

        if (!response.IsSuccessStatusCode)
        {
            var failureMessage = await GetFailureMessageAsync(response, options, ct);
            return Result<TValue>.Failure(
                CreateHttpFailureError(response.StatusCode, errorContext, failureMessage, options));
        }

        string responseBody;
        try
        {
            responseBody = await ReadResponseBodyAsync(response, options.MaxResponseBodyBytes, ct);
        }
        catch (ResponseBodyTooLargeException exception)
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(errorContext, exception,
                "The external service returned a response body that was too large to inspect."));
        }

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
                $" ({response.StatusCode}) with unsupported content type '{response.Content?.Headers.ContentType?.MediaType ?? "unknown"}'."));
        }

        try
        {
            var value = JsonSerializer.Deserialize<TValue>(
                responseBody,
                options.JsonSerializerOptions ?? DefaultJsonSerializerOptions);

            return value is null
                ? Result<TValue>.Failure(HttpError.MalformedResponse(
                    errorContext,
                    detail: $"The external service returned {(int)response.StatusCode}" +
                    $" ({response.StatusCode}) with an empty or invalid JSON body."))
                : Result<TValue>.Success(value);
        }
        catch (JsonException exception)
        {
            return Result<TValue>.Failure(
                HttpError.MalformedResponse(errorContext, exception, "The external service returned malformed JSON."));
        }
        catch (NotSupportedException exception)
        {
            return Result<TValue>.Failure(
                HttpError.MalformedResponse(errorContext, exception, "The external service returned an unsupported JSON payload."));
        }
    }

    /// <summary>
    /// Maps a successful response with an application-owned streaming deserializer or an HTTP failure.
    /// </summary>
    /// <remarks>
    /// The deserializer consumes a size-limited response stream. The response content is consumed and
    /// disposed when this method completes.
    /// </remarks>
    /// <param name="response">The response to consume, or <see langword="null"/> for a transport failure.</param>
    /// <param name="errorContext">The operation and entity context for generated errors.</param>
    /// <param name="deserializeAsync">The application-owned stream deserializer.</param>
    /// <param name="options">The upstream-message options, or <see langword="null"/> for defaults.</param>
    /// <param name="ct">The token used to cancel response-body consumption.</param>
    /// <returns>A typed result containing the deserialized value or an HTTP error.</returns>
    public static async Task<Result<TValue>> MapToResultWithDeserializerAsync<TValue>(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        Func<Stream, CancellationToken, Task<TValue>> deserializeAsync,
        HttpResponseMappingOptions? options = null,
        CancellationToken ct = default)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(deserializeAsync);
        options ??= HttpResponseMappingOptions.Default;
        options.Validate();

        if (response is null)
            return Result<TValue>.Failure(HttpError.TransportFailure(errorContext));

        if (!response.IsSuccessStatusCode)
        {
            var failureMessage = await GetFailureMessageAsync(response, options, ct);
            return Result<TValue>.Failure(
                CreateHttpFailureError(response.StatusCode, errorContext, failureMessage, options));
        }

        if (!HasJsonContentType(response))
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(
                errorContext,
                detail: $"The external service returned {(int)response.StatusCode}" +
                $" ({response.StatusCode}) with unsupported content type '{response.Content?.Headers.ContentType?.MediaType ?? "unknown"}'."));
        }

        if (response.Content is null)
            return Result<TValue>.Failure(HttpError.MalformedResponse(errorContext,
                detail: "The external service returned no response body."));

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var limitedStream = new ResponseBodyLimitStream(stream, options.MaxResponseBodyBytes);
        try
        {
            return Result<TValue>.Success(await deserializeAsync(limitedStream, ct));
        }
        catch (ResponseBodyTooLargeException exception)
        {
            return Result<TValue>.Failure(HttpError.MalformedResponse(errorContext, exception,
                "The external service returned a response body that was too large to inspect."));
        }
        catch (JsonException exception)
        {
            return Result<TValue>.Failure(
                HttpError.MalformedResponse(errorContext, exception, "The external service returned malformed JSON."));
        }
        catch (NotSupportedException exception)
        {
            return Result<TValue>.Failure(
                HttpError.MalformedResponse(errorContext, exception, "The external service returned an unsupported JSON payload."));
        }
    }

    /// <summary>Maps a successful response or an HTTP failure to a non-generic result.</summary>
    /// <param name="response">The response to consume, or <see langword="null"/> for a transport failure.</param>
    /// <param name="errorContext">The operation and entity context for generated errors.</param>
    /// <param name="ct">The token used to cancel response-body consumption.</param>
    /// <returns>A result indicating success or the mapped HTTP error.</returns>
    public static Task<Result> MapToResultAsync(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        CancellationToken ct = default) =>
        MapToResultAsync(response, errorContext, HttpResponseMappingOptions.Default, ct);

    /// <summary>Maps a response using configured upstream-message options.</summary>
    /// <param name="response">The response to consume, or <see langword="null"/> for a transport failure.</param>
    /// <param name="errorContext">The operation and entity context for generated errors.</param>
    /// <param name="options">The upstream-message options.</param>
    /// <param name="ct">The token used to cancel response-body consumption.</param>
    /// <returns>A result indicating success or the mapped HTTP error.</returns>
    public static async Task<Result> MapToResultAsync(
        this HttpResponseMessage? response,
        ErrorContext errorContext,
        HttpResponseMappingOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (response is null)
            return Result.Failure(HttpError.TransportFailure(errorContext));

        if (response.IsSuccessStatusCode)
            return Result.Success();

        var failureMessage = await GetFailureMessageAsync(response, options, ct);
        return Result.Failure(CreateHttpFailureError(response.StatusCode, errorContext, failureMessage, options));
    }

    /// <summary>
    /// Converts an HTTP request or timeout exception into a transport failure result.
    /// </summary>
    /// <remarks>
    /// Call this in the catch block around <see cref="HttpClient"/> calls. Caller cancellation is
    /// never converted to a failure; pass the caller token so cancellation can be rethrown.
    /// </remarks>
    /// <param name="exception">The transport exception to map.</param>
    /// <param name="errorContext">The operation and entity context for the generated error.</param>
    /// <param name="callerCancellationToken">The caller token used to distinguish cancellation.</param>
    /// <returns>A failed result containing a transport error.</returns>
    public static Result<TValue> MapTransportExceptionToResult<TValue>(
        this Exception exception,
        ErrorContext errorContext,
        CancellationToken callerCancellationToken = default)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errorContext);

        if (callerCancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(callerCancellationToken);

        if (exception is not HttpRequestException
            && exception is not TimeoutException
            && exception is not TaskCanceledException)
        {
            throw new ArgumentException(
                "Only HttpRequestException, TimeoutException, and non-caller TaskCanceledException can be mapped.",
                nameof(exception));
        }

        return Result<TValue>.Failure(HttpError.TransportFailure(errorContext, exception));
    }

    /// <summary>Converts an HTTP request or timeout exception into a transport failure result.</summary>
    /// <param name="exception">The transport exception to map.</param>
    /// <param name="errorContext">The operation and entity context for the generated error.</param>
    /// <param name="callerCancellationToken">The caller token used to distinguish cancellation.</param>
    /// <returns>A failed result containing a transport error.</returns>
    public static Result MapTransportExceptionToResult(
        this Exception exception,
        ErrorContext errorContext,
        CancellationToken callerCancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errorContext);

        if (callerCancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(callerCancellationToken);

        if (exception is not HttpRequestException
            && exception is not TimeoutException
            && exception is not TaskCanceledException)
        {
            throw new ArgumentException(
                "Only HttpRequestException, TimeoutException, and non-caller TaskCanceledException can be mapped.",
                nameof(exception));
        }

        return Result.Failure(HttpError.TransportFailure(errorContext, exception));
    }

    private static HttpError CreateHttpFailureError(
        HttpStatusCode statusCode,
        ErrorContext errorContext,
        string responseMessage,
        HttpResponseMappingOptions options) =>
        HttpError.FromResponse(
            errorContext,
            statusCode,
            responseMessage,
            options.UserMessageFactory?.Invoke(responseMessage));

    private static async Task<string> GetFailureMessageAsync(
        HttpResponseMessage response,
        HttpResponseMappingOptions options,
        CancellationToken ct)
    {
        string? responseMessage = null;
        try
        {
            var responseBody = await ReadResponseBodyAsync(response, options.MaxResponseBodyBytes, ct);
            responseMessage = options.ErrorMessageExtractor?.Invoke(responseBody)
                ?? ExtractDefaultResponseMessage(responseBody);
        }
        catch (ResponseBodyTooLargeException)
        {
            // Do not inspect or expose an oversized upstream body.
        }

        return BuildFailureMessage(responseMessage, GetDefaultFailureMessage(response.StatusCode), response.ReasonPhrase);
    }

    private static string? ExtractDefaultResponseMessage(string responseBody)
    {
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
            return responseBody.Trim();
        }

        return responseBody.Trim();
    }

    private static string? ExtractErrors(JsonElement errorsElement)
    {
        if (errorsElement.ValueKind == JsonValueKind.Array)
        {
            var errors = errorsElement.EnumerateArray()
                .Where(static value => value.ValueKind == JsonValueKind.String)
                .Select(static value => value.GetString())
                .Where(static value => !string.IsNullOrWhiteSpace(value));
            return string.Join(" ", errors);
        }

        if (errorsElement.ValueKind == JsonValueKind.Object)
        {
            var errors = errorsElement.EnumerateObject()
                .SelectMany(static property => property.Value.ValueKind == JsonValueKind.Array
                    ? property.Value.EnumerateArray()
                        .Where(static value => value.ValueKind == JsonValueKind.String)
                        .Select(static value => value.GetString())
                    : Array.Empty<string?>())
                .Where(static value => !string.IsNullOrWhiteSpace(value));
            return string.Join(" ", errors);
        }

        return null;
    }

    private static string BuildFailureMessage(string? responseMessage, string defaultMessage, string? reasonPhrase) =>
        FirstNonEmpty(responseMessage, defaultMessage, reasonPhrase,
            "An error occurred while calling the external service.");

    private static string GetDefaultFailureMessage(HttpStatusCode statusCode) =>
        statusCode switch
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

    private static bool HasJsonContentType(HttpResponseMessage response)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType;
        return string.IsNullOrWhiteSpace(mediaType)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadResponseBodyAsync(
        HttpResponseMessage response,
        int maxBytes,
        CancellationToken ct)
    {
        if (response.Content is null)
            return string.Empty;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        var totalBytes = 0;

        while (true)
        {
            var bytesToRead = Math.Min(buffer.Length, maxBytes - totalBytes + 1);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead), ct);
            if (bytesRead == 0)
                break;

            totalBytes += bytesRead;
            if (totalBytes > maxBytes)
                throw new ResponseBodyTooLargeException(maxBytes);

            memory.Write(buffer, 0, bytesRead);
        }

        var encoding = GetResponseEncoding(response);
        return encoding.GetString(memory.ToArray());
    }

    private static Encoding GetResponseEncoding(HttpResponseMessage response)
    {
        var charset = response.Content?.Headers.ContentType?.CharSet;
        if (string.IsNullOrWhiteSpace(charset))
            return Encoding.UTF8;

        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
        ?? "An error occurred while calling the external service.";

    private sealed class ResponseBodyTooLargeException(int maxBytes)
        : InvalidOperationException($"The response body exceeds the configured {maxBytes}-byte limit.");

    private sealed class ResponseBodyLimitStream(Stream innerStream, int maxBytes) : Stream
    {
        private int bytesRead;

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Task.FromException(new NotSupportedException());
        public override int Read(byte[] buffer, int offset, int count) =>
            CountBytes(innerStream.Read(buffer, offset, LimitReadCount(count)));
        public override int Read(Span<byte> buffer) =>
            CountBytes(innerStream.Read(buffer[..LimitReadCount(buffer.Length)]));
        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            CountBytes(await innerStream.ReadAsync(buffer[..LimitReadCount(buffer.Length)], cancellationToken));
        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            CountBytes(await innerStream.ReadAsync(buffer, offset, LimitReadCount(count), cancellationToken));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // The caller owns the underlying stream.
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private int LimitReadCount(int requestedCount) =>
            Math.Min(requestedCount, maxBytes - bytesRead + 1);

        private int CountBytes(int count)
        {
            bytesRead += count;
            if (bytesRead > maxBytes)
                throw new ResponseBodyTooLargeException(maxBytes);

            return count;
        }
    }
}
