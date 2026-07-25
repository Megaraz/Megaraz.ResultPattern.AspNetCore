# Megaraz.ResultPattern.AspNetCore

HTTP and ASP.NET Core extensions for
[`Megaraz.ResultPattern`](https://github.com/Megaraz/Megaraz.ResultPattern).
The package maps core results to predictable HTTP response descriptors and
provides optional MVC and minimal-API adapters.

The framework-agnostic mapper does not create endpoints, choose application
messages, or impose a remote service's error schema. Applications own routing,
authentication, localization, and any public API contract that differs from
the documented defaults.

## Supported frameworks

- .NET 8
- .NET 9
- .NET 10

## Installation

```bash
dotnet add package Megaraz.ResultPattern.AspNetCore
```

## Mapping outbound results

Use the pure mapper when the application needs to control how the response is
written:

```csharp
var response = HttpResultMapper.ToHttpResponse(result);
return Results.Json(
    response.Body,
    statusCode: response.StatusCode,
    headers: response.Location is null
        ? null
        : new HeaderDictionary { ["Location"] = response.Location });
```

Successful typed results use `200 OK` and place the value in the body.
Successful non-generic results use `200 OK` without a body. Created results use
`201 Created` and preserve an optional `Location` header. Commands can use
`ToNoContentResponse` for `204 No Content`.

### Minimal APIs

```csharp
app.MapGet("/accounts/{id}", (Guid id) =>
{
    Result<Account> result = service.Get(id);
    return result.ToHttpResult();
});

app.MapPost("/accounts", (CreateAccount request) =>
{
    Result<Account> result = service.Create(request);
    return result.ToCreatedHttpResult($"/accounts/{result.Value.Id}");
});
```

Use `ToNoContentHttpResult` for successful commands. These adapters write the
status, body, and `Location` header while preserving the default failure body.

### MVC controllers

```csharp
[HttpGet("{id}")]
public ActionResult<Account> Get(Guid id)
{
    Result<Account> result = service.Get(id);
    return this.ToActionResult(result);
}

[HttpPost]
public ActionResult<Account> Create(CreateAccount request)
{
    Result<Account> result = service.Create(request);
    return this.ToCreatedResult(result, $"/accounts/{result.Value.Id}");
}
```

## Default response contract

Failure responses use immutable DTOs with camel-case JSON names. Codes are
machine-readable public API identifiers; applications should version them
deliberately and avoid changing a code's meaning after publishing.

Non-validation failure:

```json
{
  "message": "Account was not found.",
  "code": "account.not_found"
}
```

Validation failure:

```json
{
  "message": "One or more validation errors occurred.",
  "code": "validation.failed",
  "validationErrors": [
    {
      "field": "email",
      "message": "Email is required."
    },
    {
      "field": null,
      "message": "The request is invalid."
    }
  ]
}
```

`validationErrors` is a materialized list and is omitted for other failures.
`MappedHttpResponse.Body` remains `object?` because successful values and custom
failure factories may use application-defined DTOs.

## Customizing mappings

Start with `HttpResultMappingPolicy.Default` and override only application
specific rules:

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

The policy supports separate selectors for core `ErrorType` and
`HttpErrorType`, success statuses for ordinary, no-content, and created
responses, and a failure-body factory. Supplying a custom failure factory makes
the application responsible for that response contract.

## Mapping inbound HTTP responses

`HttpResponseMessage` can be converted to a typed or non-generic result:

```csharp
var result = await response.MapToResultAsync<Account>(context);
```

Successful JSON bodies are deserialized with web-default
`JsonSerializerOptions`. Non-success responses become `HttpError` values.
`HttpError` retains the core package's `ErrorType.External` classification and
adds HTTP-specific categories such as `TransportFailure`,
`MalformedResponse`, and `UnexpectedStatusCode`.

Response content is consumed. The default reader inspects at most 64 KiB;
configure `HttpResponseMappingOptions.MaxResponseBodyBytes` for another limit.
Use `MapToResultWithDeserializerAsync` when a successful payload should be
streamed. Cancellation is propagated and is never converted to a failure.

Upstream response text is kept in the technical `Error.Description`; it is not
copied to `Error.UserMessage` by default. Applications that intentionally proxy
a vetted upstream message can opt in:

```csharp
var options = new HttpResponseMappingOptions
{
    UserMessageFactory = message => IsSafeForClients(message) ? message : null
};

var result = await response.MapToResultAsync<Account>(context, options);
```

Pass `JsonSerializerOptions` to control successful-body deserialization and
`ErrorMessageExtractor` when an upstream service uses a different error schema.
`HttpClient` request, DNS, connection, and timeout exceptions are not caught by
the response mapper; map selected exceptions explicitly in the surrounding
catch block with `MapTransportExceptionToResult`.

## Pagination

Normalize endpoint query values with an application-configured maximum:

```csharp
var pagination = PaginationParameters.Normalize(page, pageSize, maxPageSize);
```

Page numbers are at least 1 and page sizes are clamped between 1 and the
provided maximum. A maximum below 1 throws `ArgumentOutOfRangeException`.

## Versioning

The package follows semantic versioning. Public response fields, error codes,
and documented mapping defaults are compatibility-sensitive API. Breaking
changes require a major version; new backward-compatible APIs and behavior
use minor versions. Patch releases contain backward-compatible fixes.

## License

This project is licensed under the [MIT License](LICENSE).
