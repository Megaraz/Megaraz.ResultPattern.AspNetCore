using Megaraz.ResultPattern;
using Megaraz.ResultPattern.AspNetCore;

namespace UnitTests;

public class HttpResultMapperTests
{
    [Fact]
    public void MapsSuccessfulTypedAndNonGenericResults()
    {
        var typed = HttpResultMapper.ToHttpResponse(Result<int>.Success(42));
        var empty = HttpResultMapper.ToHttpResponse(Result.Success());
        var noContent = HttpResultMapper.ToNoContentResponse(Result.Success());
        var created = HttpResultMapper.ToCreatedResponse(Result<string>.Success("created"), "/accounts/1");

        Assert.Equal(200, typed.StatusCode);
        Assert.Equal(42, typed.Body);
        Assert.Equal(200, empty.StatusCode);
        Assert.Null(empty.Body);
        Assert.Equal(204, noContent.StatusCode);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("/accounts/1", created.Location);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 422)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Failure, 500)]
    [InlineData(ErrorType.Cancelled, 503)]
    [InlineData(ErrorType.External, 502)]
    public void MapsCoreFailureTypes(ErrorType type, int statusCode)
    {
        var error = type == ErrorType.Validation
            ? ValidationError.Required(TestData.FieldContext, "Email is required.")
            : Megaraz.ResultPattern.Error.Custom("code", "details", type);
        var result = type == ErrorType.Validation
            ? Result<int>.ValidationFailure(new[] { (ValidationError)error }, "Invalid request.")
            : Result<int>.Failure(error);

        var response = HttpResultMapper.ToHttpResponse(result);

        Assert.Equal(statusCode, response.StatusCode);
        Assert.NotNull(response.Body);
        if (type == ErrorType.Validation)
            Assert.IsType<ValidationErrorResponseBody>(response.Body);
        else
            Assert.IsType<ErrorResponseBody>(response.Body);
    }

    [Theory]
    [InlineData(HttpErrorType.BadRequest, 400)]
    [InlineData(HttpErrorType.Unauthorized, 401)]
    [InlineData(HttpErrorType.Forbidden, 403)]
    [InlineData(HttpErrorType.NotFound, 404)]
    [InlineData(HttpErrorType.Conflict, 409)]
    [InlineData(HttpErrorType.UnprocessableContent, 422)]
    [InlineData(HttpErrorType.TooManyRequests, 429)]
    [InlineData(HttpErrorType.TransportFailure, 503)]
    [InlineData(HttpErrorType.InternalServerError, 502)]
    [InlineData(HttpErrorType.MalformedResponse, 502)]
    [InlineData(HttpErrorType.UnexpectedStatusCode, 502)]
    public void MapsHttpFailureTypes(HttpErrorType type, int statusCode)
    {
        var error = type switch
        {
            HttpErrorType.BadRequest => HttpError.BadRequest(TestData.Context),
            HttpErrorType.Unauthorized => HttpError.UnauthorizedAccess(TestData.Context),
            HttpErrorType.Forbidden => HttpError.Forbidden(TestData.Context),
            HttpErrorType.NotFound => HttpError.NotFound(TestData.Context),
            HttpErrorType.Conflict => HttpError.Conflict(TestData.Context),
            HttpErrorType.UnprocessableContent => HttpError.UnprocessableContent(TestData.Context),
            HttpErrorType.TooManyRequests => HttpError.TooManyRequests(TestData.Context),
            HttpErrorType.TransportFailure => HttpError.TransportFailure(TestData.Context),
            HttpErrorType.InternalServerError => HttpError.InternalServerError(TestData.Context),
            HttpErrorType.MalformedResponse => HttpError.MalformedResponse(TestData.Context),
            _ => HttpError.UnexpectedStatusCode(TestData.Context, (System.Net.HttpStatusCode)418)
        };

        Assert.Equal(statusCode, HttpResultMapper.ToHttpResponse(Result.Failure(error)).StatusCode);
    }

    [Fact]
    public void MapsValidationItemsAndMaterializesThem()
    {
        var first = ValidationError.Required(TestData.FieldContext, "Required");
        var second = ValidationError.Custom(TestData.Context, "General", "General");
        var result = Result.ValidationFailure(new[] { first, second }, "Invalid");

        var body = Assert.IsType<ValidationErrorResponseBody>(
            HttpResultMapper.ToHttpResponse(result).Body);

        Assert.Equal(new[]
        {
            new ValidationErrorItem("Email", "Required"),
            new ValidationErrorItem(null, "General")
        }, body.ValidationErrors);
    }

    [Fact]
    public void AppliesCustomPolicy()
    {
        var policy = HttpResultMappingPolicy.Default with
        {
            SuccessStatusCode = 202,
            ErrorTypeStatusCode = _ => 418,
            FailureBodyFactory = _ => "custom"
        };

        Assert.Equal(202, HttpResultMapper.ToHttpResponse(Result.Success(), policy).StatusCode);
        var failure = HttpResultMapper.ToHttpResponse(Result.Failure(TestData.Error), policy);
        Assert.Equal(418, failure.StatusCode);
        Assert.Equal("custom", failure.Body);
    }
}
