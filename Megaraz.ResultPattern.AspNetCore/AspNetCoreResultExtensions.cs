using Megaraz.ResultPattern;
using Microsoft.AspNetCore.Mvc;

namespace Megaraz.ResultPattern.AspNetCore;

/// <summary>ASP.NET Core MVC extensions for converting results to action results.</summary>
public static class AspNetCoreResultExtensions
{
    /// <summary>Maps a typed result to an <see cref="ActionResult{TValue}"/>.</summary>
    public static ActionResult<TValue> ToActionResult<TValue>(
        this ControllerBase controller,
        Result<TValue> result,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return controller.ToTypedActionResult<TValue>(HttpResultMapper.ToHttpResponse(result, policy));
    }

    /// <summary>Maps a result to an <see cref="IActionResult"/>.</summary>
    public static IActionResult ToActionResult(
        this ControllerBase controller,
        Result result,
        HttpResultMappingPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return controller.ToActionResult(HttpResultMapper.ToHttpResponse(result, policy));
    }

    /// <summary>Maps a result to a 204 No Content response on success.</summary>
    public static IActionResult ToNoContentResult(
        this ControllerBase controller,
        Result result,
        HttpResultMappingPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return controller.ToActionResult(HttpResultMapper.ToNoContentResponse(result, policy));
    }

    /// <summary>Maps a typed result to a 201 Created response on success.</summary>
    /// <param name="controller">The MVC controller creating the response.</param>
    /// <param name="result">The result to map.</param>
    /// <param name="location">The URI of the created resource, when available.</param>
    /// <param name="policy">The HTTP mapping policy, or <see langword="null"/> for the default policy.</param>
    public static ActionResult<TValue> ToCreatedResult<TValue>(
        this ControllerBase controller,
        Result<TValue> result,
        string? location = null,
        HttpResultMappingPolicy? policy = null)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        var response = result.IsSuccess
            ? HttpResultMapper.ToCreatedResponse(result, location, policy)
            : HttpResultMapper.ToHttpResponse(result, policy);

        return controller.ToTypedActionResult<TValue>(response);
    }

    private static ActionResult<TValue> ToTypedActionResult<TValue>(
        this ControllerBase controller,
        MappedHttpResponse response)
        where TValue : notnull
    {
        return response.StatusCode switch
        {
            200 => controller.Ok(response.Body),
            201 when response.Location is not null =>
                new ActionResult<TValue>(controller.Created(response.Location, response.Body)),
            204 => new ActionResult<TValue>(controller.NoContent()),
            _ => new ActionResult<TValue>(controller.StatusCode(response.StatusCode, response.Body))
        };
    }

    private static IActionResult ToActionResult(
        this ControllerBase controller,
        MappedHttpResponse response)
    {
        return response.StatusCode switch
        {
            200 when response.Body is not null => controller.Ok(response.Body),
            200 => controller.Ok(),
            201 when response.Location is not null => controller.Created(response.Location, response.Body),
            204 => controller.NoContent(),
            _ => controller.StatusCode(response.StatusCode, response.Body)
        };
    }
}
