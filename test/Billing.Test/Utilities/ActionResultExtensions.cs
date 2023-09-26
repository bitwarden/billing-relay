using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Test.Utilities;

public static class ActionResultExtensions
{
    public static void CheckFor(this IActionResult result, int statusCode)
    {
        var statusCodeResult = result as StatusCodeResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(statusCode);
    }

    public static void CheckFor(this IActionResult result, int statusCode, string response)
    {
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(statusCode);
        objectResult.Value.Should().Be(response);
    }
}
