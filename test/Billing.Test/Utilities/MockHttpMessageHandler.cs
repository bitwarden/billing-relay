using System.Net;

namespace Billing.Test.Utilities;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public int Invocations { get; private set; }
    public string? RequestContent { get; private set; }
    public string? RequestUri { get; private set; }

    public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Invocations++;

        RequestUri = request.RequestUri?.ToString();

        if (request.Content != null)
        {
            RequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_responseContent)
        };
    }
}
