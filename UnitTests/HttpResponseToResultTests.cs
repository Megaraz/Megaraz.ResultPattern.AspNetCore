using System.Net;
using System.Text;
using System.Text.Json;
using Megaraz.ResultPattern;
using Megaraz.ResultPattern.AspNetCore;

namespace UnitTests;

public class HttpResponseToResultTests
{
    [Fact]
    public async Task MapsNullResponseToTransportFailure()
    {
        var result = await ((HttpResponseMessage?)null).MapToResultAsync<string>(TestData.Context);

        Assert.True(result.IsFailure);
        Assert.Equal(HttpErrorType.TransportFailure, Assert.IsType<HttpError>(result.PrimaryError).HttpErrorType);
    }

    [Fact]
    public async Task MapsJsonSuccessWithWebDefaults()
    {
        using var response = TestData.Response(HttpStatusCode.OK, """{"displayName":"Ada"}""");

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Value.DisplayName);
    }

    [Fact]
    public async Task UsesConfiguredJsonOptions()
    {
        using var response = TestData.Response(HttpStatusCode.OK, """{"display_name":"Ada"}""");
        var options = new HttpResponseMappingOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }
        };

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context, options);

        Assert.Equal("Ada", result.Value.DisplayName);
    }

    [Theory]
    [InlineData(null, "without the expected response body")]
    [InlineData("", "without the expected response body")]
    [InlineData("not-json", "malformed JSON")]
    public async Task MapsEmptyAndMalformedSuccessResponses(string? body, string expectedDetail)
    {
        using var response = TestData.Response(HttpStatusCode.OK, body);
        if (body == "not-json")
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context);

        var error = Assert.IsType<HttpError>(result.PrimaryError);
        Assert.Equal(HttpErrorType.MalformedResponse, error.HttpErrorType);
        Assert.Contains(expectedDetail, error.Description);
    }

    [Fact]
    public async Task RejectsUnsupportedSuccessContentType()
    {
        using var response = TestData.Response(HttpStatusCode.OK, """{"displayName":"Ada"}""", "text/plain");

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context);

        Assert.Equal(HttpErrorType.MalformedResponse, Assert.IsType<HttpError>(result.PrimaryError).HttpErrorType);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{"message":"Invalid account"}""", "Invalid account")]
    [InlineData(HttpStatusCode.NotFound, """{"detail":"Missing account"}""", "Missing account")]
    [InlineData(HttpStatusCode.BadRequest, """{"errors":["First","Second"]}""", "First Second")]
    [InlineData(HttpStatusCode.BadRequest, """{"errors":{"Email":["Required","Invalid"]}}""", "Required Invalid")]
    [InlineData(HttpStatusCode.BadRequest, "upstream text", "upstream text")]
    public async Task ExtractsFailureMessagesWithoutExposingThemAsUserMessages(
        HttpStatusCode statusCode,
        string body,
        string expectedMessage)
    {
        using var response = TestData.Response(statusCode, body);

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context);
        var error = Assert.IsType<HttpError>(result.PrimaryError);

        Assert.Contains(expectedMessage, error.Description);
        Assert.Empty(error.UserMessage);
        Assert.Empty(result.Message);
    }

    [Fact]
    public async Task ConfiguresFailureExtractionAndSafeUserMessage()
    {
        using var response = TestData.Response(HttpStatusCode.BadRequest, """{"problem":"upstream"}""");
        var options = new HttpResponseMappingOptions
        {
            ErrorMessageExtractor = _ => "application message",
            UserMessageFactory = message => $"Safe: {message}"
        };

        var result = await response.MapToResultAsync<SuccessDto>(TestData.Context, options);

        Assert.Equal("Safe: application message", result.Message);
        Assert.Equal("Safe: application message", result.PrimaryError.UserMessage);
    }

    [Fact]
    public async Task MapsNonGenericSuccessAndFailure()
    {
        using var success = TestData.Response(HttpStatusCode.NoContent);
        using var failure = TestData.Response(HttpStatusCode.Conflict, """{"title":"Conflict"}""");

        Assert.True((await success.MapToResultAsync(TestData.Context)).IsSuccess);
        Assert.Equal(HttpErrorType.Conflict,
            Assert.IsType<HttpError>((await failure.MapToResultAsync(TestData.Context)).PrimaryError).HttpErrorType);
    }

    [Fact]
    public async Task PropagatesCallerCancellation()
    {
        using var response = TestData.Response(HttpStatusCode.BadRequest, """{"message":"Invalid"}""");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            response.MapToResultAsync<SuccessDto>(TestData.Context, cts.Token));
    }

    [Fact]
    public async Task UsesStreamingDeserializer()
    {
        using var response = TestData.Response(HttpStatusCode.OK, """{"displayName":"Ada"}""");
        var result = await response.MapToResultWithDeserializerAsync(
            TestData.Context,
            async (stream, ct) =>
                (await JsonSerializer.DeserializeAsync<SuccessDto>(
                    stream,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    ct))!);

        Assert.Equal("Ada", result.Value.DisplayName);
    }

    [Fact]
    public async Task RejectsOversizedStreamingResponse()
    {
        using var response = TestData.Response(HttpStatusCode.OK, "{" + new string(' ', 32) + "}");
        var options = new HttpResponseMappingOptions { MaxResponseBodyBytes = 16 };

        var result = await response.MapToResultWithDeserializerAsync(
            TestData.Context,
            async (stream, ct) =>
            {
                await stream.CopyToAsync(Stream.Null, ct);
                return new SuccessDto();
            },
            options);

        var error = Assert.IsType<HttpError>(result.PrimaryError);
        Assert.Equal(HttpErrorType.MalformedResponse, error.HttpErrorType);
        Assert.Contains("too large", error.Description);
    }

    [Fact]
    public async Task StreamingDeserializerPreservesCancellation()
    {
        using var response = TestData.Response(HttpStatusCode.OK, "{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            response.MapToResultWithDeserializerAsync(
                TestData.Context,
                (_, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult(new SuccessDto());
                },
                ct: cts.Token));
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(TaskCanceledException))]
    public void MapsTransportExceptions(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        var result = exception.MapTransportExceptionToResult<string>(TestData.Context);

        Assert.Equal(HttpErrorType.TransportFailure,
            Assert.IsType<HttpError>(result.PrimaryError).HttpErrorType);
        Assert.Same(exception, result.PrimaryError.Exception);
    }

    [Fact]
    public void RejectsUnsupportedTransportExceptionsAndPreservesCancellation()
    {
        Assert.Throws<ArgumentException>(() =>
            new InvalidOperationException().MapTransportExceptionToResult(TestData.Context));

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            new HttpRequestException().MapTransportExceptionToResult(TestData.Context, cts.Token));
    }

    public sealed record SuccessDto
    {
        public string DisplayName { get; init; } = string.Empty;
    }
}
