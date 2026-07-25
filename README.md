# Megaraz.ResultPattern.AspNetCore

ASP.NET Core and HTTP extensions for
[`Megaraz.ResultPattern`](https://github.com/Megaraz/Megaraz.ResultPattern).

## HTTP response mapping

Map a domain result to a framework-agnostic response descriptor:

```csharp
var response = HttpResultMapper.ToHttpResponse(result);
return Results.Json(response.Body, statusCode: response.StatusCode);
```

## ASP.NET Core MVC

Controller actions can map results directly to MVC action results:

```csharp
[HttpGet("{id}")]
public ActionResult<Account> Get(Guid id)
{
    Result<Account> result = service.Get(id);
    return this.ToActionResult(result);
}
```

Use `ToCreatedResult(result, location)` for successful create operations and
`ToNoContentResult(result)` for successful commands.

## Minimal APIs

Minimal API handlers can return the provided `IResult` adapters:

```csharp
app.MapGet("/accounts/{id}", (Guid id) =>
{
    Result<Account> result = service.Get(id);
    return result.ToHttpResult();
});
```

Use `ToCreatedHttpResult(result, location)` and
`ToNoContentHttpResult()` for create and command endpoints.

## Customizing HTTP mappings

The default mappings can be customized without replacing the mapper. Start with
`HttpResultMappingPolicy.Default` and override only the application-specific
rules:

```csharp
var policy = HttpResultMappingPolicy.Default with
{
    SuccessStatusCode = StatusCodes.Status202Accepted,
    ErrorTypeStatusCode = errorType => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => HttpResultMappingPolicy.Default.ErrorTypeStatusCode(errorType)
    },
    FailureBodyFactory = result =>
        new { error = result.Message, code = result.PrimaryError.Code }
};

return result.ToHttpResult(policy);
```

The policy provides separate selectors for core `ErrorType` and
`HttpErrorType` values, success status codes for ordinary, no-content, and
created responses, and a failure-body factory. Calls that do not provide a
policy retain the standard status codes and response bodies.

`HttpError` extends the core package's `ErrorType.External` classification and
adds HTTP-specific categories such as `TransportFailure`, `MalformedResponse`,
and `UnexpectedStatusCode`. `HttpResponseMessage` responses can be converted
back to `Result` values with `MapToResultAsync`. Upstream response text is kept
in the technical `Error.Description`; by default it is not copied into
`Error.UserMessage` or `Result.Message`:

```csharp
var result = await response.MapToResultAsync<Account>(context);
```

If an application deliberately proxies a vetted upstream message, opt in with
an application-owned policy:

```csharp
var options = new HttpResponseMappingOptions
{
    UserMessageFactory = message => IsSafeForClients(message) ? message : null
};

var result = await response.MapToResultAsync<Account>(context, options);
```

Install from NuGet:

```bash
dotnet add package Megaraz.ResultPattern.AspNetCore
```