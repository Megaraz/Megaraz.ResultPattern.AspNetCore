using System.Net;
using System.Text;
using Megaraz.ResultPattern;

namespace UnitTests;

internal static class TestData
{
    public static readonly ErrorContext Context = new(OperationType.Get, "Account");
    public static readonly ErrorContext FieldContext = new(OperationType.Update, "Account", "Email");
    public static readonly Error Error =
        Megaraz.ResultPattern.Error.Custom("account.failure", "Technical failure.", ErrorType.Failure);

    public static HttpResponseMessage Response(
        HttpStatusCode statusCode,
        string? body = null,
        string mediaType = "application/json")
    {
        var response = new HttpResponseMessage(statusCode);
        if (body is not null)
        {
            response.Content = new StringContent(body, Encoding.UTF8, mediaType);
        }

        return response;
    }
}
