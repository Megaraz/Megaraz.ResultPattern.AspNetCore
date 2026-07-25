using System.Net;
using Megaraz.ResultPattern;
using Megaraz.ResultPattern.AspNetCore;

namespace UnitTests;

public class PaginationAndContractsTests
{
    [Theory]
    [InlineData(0, 0, 25, 1, 1)]
    [InlineData(3, 10, 25, 3, 10)]
    [InlineData(3, 100, 25, 3, 25)]
    public void NormalizeClampsPageValues(int page, int size, int max, int expectedPage, int expectedSize)
    {
        var result = PaginationParameters.Normalize(page, size, max);

        Assert.Equal(expectedPage, result.PageNumber);
        Assert.Equal(expectedSize, result.PageSize);
    }

    [Fact]
    public void NormalizeRejectsInvalidMaximum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaginationParameters.Normalize(1, 1, 0));
    }

    [Fact]
    public void ValidationResponseBodyCopiesItemsAndRejectsNullInputs()
    {
        var items = new List<ValidationErrorItem> { new("Email", "Required") };
        var body = new ValidationErrorResponseBody("Invalid", "validation.failed", items);
        items[0] = new("Name", "Different");

        Assert.Equal("Email", body.ValidationErrors[0].Field);
        Assert.Throws<ArgumentNullException>(() => new ValidationErrorResponseBody(null!, "code", []));
        Assert.Throws<ArgumentNullException>(() => new ValidationErrorResponseBody("message", null!, []));
        Assert.Throws<ArgumentNullException>(() => new ValidationErrorResponseBody("message", "code", null!));
    }

    [Fact]
    public async Task MappingOptionsRejectInvalidBodyLimit()
    {
        var response = TestData.Response(HttpStatusCode.OK, """{"value":1}""");
        var options = new HttpResponseMappingOptions { MaxResponseBodyBytes = 0 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            response.MapToResultAsync<int>(TestData.Context, options));
    }

    [Fact]
    public void MappingPolicyRejectsNullCallbacks()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HttpResultMapper.ToHttpResponse(Result.Success(), new HttpResultMappingPolicy
            {
                ErrorTypeStatusCode = null!
            }));
        Assert.Throws<ArgumentNullException>(() =>
            HttpResultMapper.ToHttpResponse(Result.Success(), new HttpResultMappingPolicy
            {
                HttpErrorTypeStatusCode = null!
            }));
        Assert.Throws<ArgumentNullException>(() =>
            HttpResultMapper.ToHttpResponse(Result.Success(), new HttpResultMappingPolicy
            {
                FailureBodyFactory = null!
            }));
    }
}
