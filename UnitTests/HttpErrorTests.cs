using System.Net;
using Megaraz.ResultPattern;
using Megaraz.ResultPattern.AspNetCore;

namespace UnitTests;

public class HttpErrorTests
{
    [Theory]
    [InlineData(HttpErrorType.Custom, "Custom")]
    [InlineData(HttpErrorType.TransportFailure, "TransportFailure")]
    [InlineData(HttpErrorType.TooManyRequests, "TooManyRequests")]
    [InlineData(HttpErrorType.MalformedResponse, "MalformedResponse")]
    [InlineData(HttpErrorType.UnexpectedStatusCode, "UnexpectedStatusCode")]
    [InlineData(HttpErrorType.UnprocessableContent, "UnprocessableContent")]
    [InlineData(HttpErrorType.BadRequest, "BadRequest")]
    [InlineData(HttpErrorType.Unauthorized, "Unauthorized")]
    [InlineData(HttpErrorType.Forbidden, "Forbidden")]
    [InlineData(HttpErrorType.NotFound, "NotFound")]
    [InlineData(HttpErrorType.Conflict, "Conflict")]
    [InlineData(HttpErrorType.InternalServerError, "InternalServerError")]
    public void FactoriesExposeTypeAndStableCode(HttpErrorType type, string reason)
    {
        var error = type switch
        {
            HttpErrorType.Custom => HttpError.Custom(TestData.Context, "custom"),
            HttpErrorType.TransportFailure => HttpError.TransportFailure(TestData.Context),
            HttpErrorType.TooManyRequests => HttpError.TooManyRequests(TestData.Context),
            HttpErrorType.MalformedResponse => HttpError.MalformedResponse(TestData.Context),
            HttpErrorType.UnexpectedStatusCode => HttpError.UnexpectedStatusCode(TestData.Context, (HttpStatusCode)418),
            HttpErrorType.UnprocessableContent => HttpError.UnprocessableContent(TestData.Context),
            HttpErrorType.BadRequest => HttpError.BadRequest(TestData.Context),
            HttpErrorType.Unauthorized => HttpError.UnauthorizedAccess(TestData.Context),
            HttpErrorType.Forbidden => HttpError.Forbidden(TestData.Context),
            HttpErrorType.NotFound => HttpError.NotFound(TestData.Context),
            HttpErrorType.Conflict => HttpError.Conflict(TestData.Context),
            _ => HttpError.InternalServerError(TestData.Context)
        };

        Assert.Equal(type, error.HttpErrorType);
        Assert.Equal($"Get.Account.{reason}", error.Code);
        Assert.Equal(ErrorType.External, error.Type);
        Assert.NotEmpty(error.Description);
    }

    [Fact]
    public void FactoriesPreserveUserMessageExceptionAndLocalization()
    {
        var exception = new TimeoutException();
        var error = HttpError.TransportFailure(
            TestData.Context,
            exception,
            "Try again.",
            context => $"Localized {context.EntityName}");

        Assert.Same(exception, error.Exception);
        Assert.Equal("Try again.", error.UserMessage);
        Assert.Equal("Localized Account", error.Description);
    }

    [Fact]
    public void CustomPreservesTechnicalAndUserValues()
    {
        var error = HttpError.Custom(TestData.Context, "remote detail", "safe message");

        Assert.Contains("remote detail", error.Description);
        Assert.Equal("safe message", error.UserMessage);
    }
}
