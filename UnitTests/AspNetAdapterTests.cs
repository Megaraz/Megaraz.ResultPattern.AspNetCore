using System.Text.Json;
using Megaraz.ResultPattern;
using Megaraz.ResultPattern.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

public class AspNetAdapterTests
{
    [Fact]
    public async Task MinimalAdapterWritesSuccessCreatedAndFailureResponses()
    {
        var context = NewContext();

        await Result<int>.Success(42).ToHttpResult().ExecuteAsync(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(42, await ReadJsonAsync<int>(context));

        context = NewContext();
        await Result<string>.Success("new").ToCreatedHttpResult("/items/1").ExecuteAsync(context);
        Assert.Equal(201, context.Response.StatusCode);
        Assert.Equal("/items/1", context.Response.Headers.Location.ToString());

        context = NewContext();
        await Result.Failure(Error.NotFound(TestData.Context)).ToHttpResult().ExecuteAsync(context);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task MinimalAdapterWritesNoContent()
    {
        var context = NewContext();

        await Result.Success().ToNoContentHttpResult().ExecuteAsync(context);

        Assert.Equal(204, context.Response.StatusCode);
    }

    [Fact]
    public void MinimalAdapterRejectsNullResult()
    {
        Result<string>? result = null;

        Assert.Throws<ArgumentNullException>(() => result!.ToHttpResult());
        Assert.Throws<ArgumentNullException>(() => ((Result?)null)!.ToHttpResult());
    }

    [Fact]
    public void MvcAdapterReturnsExpectedActionResults()
    {
        var controller = new TestController();

        var ok = controller.ToActionResult(Result<int>.Success(42));
        var created = controller.ToCreatedResult(Result<string>.Success("new"), "/items/1");
        var createdAtAction = controller.ToCreatedResult(
            Result<int>.Success(42),
            "GetById",
            value => new { id = value });
        var noContent = controller.ToNoContentResult(Result.Success());
        var failure = controller.ToActionResult(Result.Failure(Error.NotFound(TestData.Context)));

        Assert.IsType<OkObjectResult>(ok.Result);
        Assert.IsType<CreatedResult>(created.Result);
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(createdAtAction.Result);
        Assert.Equal("GetById", createdAtActionResult.ActionName);
        Assert.Equal(42, createdAtActionResult.RouteValues!["id"]);
        Assert.Equal(42, createdAtActionResult.Value);
        Assert.IsType<NoContentResult>(noContent);
        Assert.Equal(404, Assert.IsType<ObjectResult>(failure).StatusCode);
    }

    [Fact]
    public void MvcCreatedAtActionAdapterMapsFailureAndAppliesCreatedStatusPolicy()
    {
        var controller = new TestController();
        var policy = HttpResultMappingPolicy.Default with { CreatedStatusCode = 202 };

        var created = controller.ToCreatedResult(
            Result<string>.Success("new"),
            "GetById",
            _ => new { id = 42 },
            policy);
        var failure = controller.ToCreatedResult(
            Result<string>.Failure(Error.NotFound(TestData.Context)),
            "GetById",
            _ => throw new InvalidOperationException("Route values must not be created for failures."));

        Assert.Equal(202, Assert.IsType<CreatedAtActionResult>(created.Result).StatusCode);
        Assert.Equal(404, Assert.IsType<ObjectResult>(failure.Result).StatusCode);
    }

    [Fact]
    public void MvcAdapterRejectsNullArguments()
    {
        var controller = new TestController();

        Assert.Throws<ArgumentNullException>(() => controller.ToActionResult((Result)null!));
        Assert.Throws<ArgumentNullException>(() => controller.ToActionResult((Result<string>)null!));
        Assert.Throws<ArgumentNullException>(() => ((ControllerBase)null!).ToActionResult(Result.Success()));
        Assert.Throws<ArgumentNullException>(() => controller.ToCreatedResult(
            Result<string>.Success("new"),
            "GetById",
            (Func<string, object>)null!));
        Assert.Throws<ArgumentException>(() => controller.ToCreatedResult(
            Result<string>.Success("new"),
            string.Empty,
            _ => new { }));
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddOptions()
            .Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(_ => { })
            .BuildServiceProvider();
        return context;
    }

    private static async Task<T> ReadJsonAsync<T>(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<T>(context.Response.Body))!;
    }

    private sealed class TestController : ControllerBase;
}
