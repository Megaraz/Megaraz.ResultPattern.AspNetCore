namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>
/// Pure mapper that converts domain <see cref="Result"/> instances
/// into framework-agnostic <see cref="MappedHttpResponse"/> descriptors.
/// No ASP.NET dependencies — only HTTP semantics.
/// </summary>
public static class HttpResultMapper
{
    /// <summary>
    /// Maps a successful <see cref="Result{TValue}"/> to 200 OK with the value as the body,
    /// or maps a failure to the appropriate HTTP error response.
    /// </summary>
    public static MappedHttpResponse ToHttpResponse<TValue>(
        Result<TValue> result,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        policy ??= HttpResultMappingPolicy.Default;
        policy.Validate();

        return result.IsSuccess
            ? new MappedHttpResponse(policy.SuccessStatusCode, result.Value)
            : MapFailure(result, policy);
    }

    /// <summary>
    /// Maps a successful <see cref="Result"/> to 200 OK (no body),
    /// or maps a failure to the appropriate HTTP error response.
    /// </summary>
    public static MappedHttpResponse ToHttpResponse(
        Result result,
        HttpResultMappingPolicy? policy = null)
    {
        policy ??= HttpResultMappingPolicy.Default;
        policy.Validate();

        return result.IsSuccess
            ? new MappedHttpResponse(policy.SuccessStatusCode)
            : MapFailure(result, policy);
    }

    /// <summary>
    /// Maps a successful <see cref="Result"/> to 204 No Content,
    /// or maps a failure to the appropriate HTTP error response.
    /// </summary>
    public static MappedHttpResponse ToNoContentResponse(
        Result result,
        HttpResultMappingPolicy? policy = null)
    {
        policy ??= HttpResultMappingPolicy.Default;
        policy.Validate();

        return result.IsSuccess
            ? new MappedHttpResponse(policy.NoContentStatusCode)
            : MapFailure(result, policy);
    }

    /// <summary>
    /// Maps a successful <see cref="Result{TValue}"/> to 201 Created with the value as the body
    /// and an optional location, or maps a failure to the appropriate HTTP error response.
    /// </summary>
    public static MappedHttpResponse ToCreatedResponse<TValue>(
        Result<TValue> result,
        string? location = null,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        policy ??= HttpResultMappingPolicy.Default;
        policy.Validate();

        return result.IsSuccess
            ? new MappedHttpResponse(policy.CreatedStatusCode, result.Value, location)
            : MapFailure(result, policy);
    }

    private static MappedHttpResponse MapFailure(Result result, HttpResultMappingPolicy policy)
    {
        var primaryError = result.PrimaryError;
        var statusCode = primaryError is HttpError httpError
            ? policy.HttpErrorTypeStatusCode(httpError.HttpErrorType)
            : policy.ErrorTypeStatusCode(primaryError.Type);

        return new MappedHttpResponse(statusCode, policy.FailureBodyFactory(result));
    }

    internal static object CreateDefaultFailureBody(Result result)
    {
        var primaryError = result.PrimaryError;
        if (primaryError.Type == ErrorType.Validation)
        {
            var validationErrorItems = result.ValidationErrors
                .Select(x => new ValidationErrorItem(x.FieldName, x.UserMessage))
                .ToArray();
            return new ValidationErrorResponseBody(result.Message, validationErrorItems);
        }

        return new ErrorResponseBody(result.Message, primaryError.Code);
    }
}
