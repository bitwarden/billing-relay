using Microsoft.AspNetCore.Mvc;

namespace Billing.Test.Utilities;

public static class ActionResultExtensions
{
    public static void CheckFor(this IActionResult result, int statusCode)
    {
        var statusCodeResult = result as StatusCodeResult;
        Assert.NotNull(statusCodeResult);
        Assert.Equal(statusCode, statusCodeResult.StatusCode);
    }

    public static void CheckFor(this IActionResult result, int statusCode, string response)
    {
        var objectResult = result as ObjectResult;
        Assert.NotNull(objectResult);
        Assert.Equal(statusCode, objectResult.StatusCode);
        Assert.Equal(response, objectResult.Value);
    }
}
