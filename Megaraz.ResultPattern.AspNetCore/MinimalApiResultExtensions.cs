using Megaraz.ResultPattern;
using Microsoft.AspNetCore.Http;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>Minimal API extensions for converting results to <see cref="IResult"/> values.</summary>
public static class MinimalApiResultExtensions
{
    /// <summary>Maps a typed result to a minimal API HTTP result.</summary>
    /// <param name="result">The result to map.</param>
    /// <param name="policy">The HTTP mapping policy, or <see langword="null"/> for the default policy.</param>
    /// <returns>An ASP.NET Core minimal API result.</returns>
    public static IResult ToHttpResult<TValue>(
        this Result<TValue> result,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        return ToHttpResult(HttpResultMapper.ToHttpResponse(result, policy));
    }

    /// <summary>Maps a result to a minimal API HTTP result.</summary>
    /// <param name="result">The result to map.</param>
    /// <param name="policy">The HTTP mapping policy, or <see langword="null"/> for the default policy.</param>
    /// <returns>An ASP.NET Core minimal API result.</returns>
    public static IResult ToHttpResult(
        this Result result,
        HttpResultMappingPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        return ToHttpResult(HttpResultMapper.ToHttpResponse(result, policy));
    }

    /// <summary>Maps a result to a 204 No Content result on success.</summary>
    /// <param name="result">The result to map.</param>
    /// <param name="policy">The HTTP mapping policy, or <see langword="null"/> for the default policy.</param>
    /// <returns>An ASP.NET Core minimal API result.</returns>
    public static IResult ToNoContentHttpResult(
        this Result result,
        HttpResultMappingPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        return ToHttpResult(HttpResultMapper.ToNoContentResponse(result, policy));
    }

    /// <summary>Maps a typed result to a 201 Created result on success.</summary>
    /// <param name="result">The result to map.</param>
    /// <param name="location">The URI of the created resource, when available.</param>
    /// <param name="policy">The HTTP mapping policy, or <see langword="null"/> for the default policy.</param>
    public static IResult ToCreatedHttpResult<TValue>(
        this Result<TValue> result,
        string? location = null,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        var response = result.IsSuccess
            ? HttpResultMapper.ToCreatedResponse(result, location, policy)
            : HttpResultMapper.ToHttpResponse(result, policy);

        return ToHttpResult(response);
    }

    private static IResult ToHttpResult(MappedHttpResponse response)
    {
        return response.StatusCode switch
        {
            200 when response.Body is not null => Results.Ok(response.Body),
            200 => Results.Ok(),
            201 when response.Location is not null => Results.Created(response.Location, response.Body),
            204 => Results.NoContent(),
            _ => Results.Json(response.Body, statusCode: response.StatusCode)
        };
    }
}
