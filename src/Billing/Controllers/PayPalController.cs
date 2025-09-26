using System.Text.RegularExpressions;
using Billing.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Billing.Controllers;

[ApiController]
[Route("paypal")]
public partial class PayPalController(
    ILogger<PayPalController> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<GlobalSettingsOptions> globalSettingOptionsSnapshot,
    Observability observability)
    : ControllerBase
{
    /// <summary>
    /// Handles the logic for routing PayPal IPN traffic depending on the datacenter
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost("ipn")]
    public async Task<IActionResult> PostIpnAsync([FromQuery] string key)
    {
        logger.LogDebug("PayPal IPN has been hit");

        var formData = Request.Form
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
            .ToList();

        var cloudRegion = GetCloudRegionFromCustomFields(formData);
        var targetUrl = GetTargetUrl(key, cloudRegion);
        var formContent = new FormUrlEncodedContent(formData);

        try
        {
            observability.TrackPayPalRequest(cloudRegion);

            using var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(targetUrl, formContent);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }

            logger.LogWarning(
                "Encountered an unexpected error while calling PayPal IPN for the region \"{CloudRegion}\"",
                cloudRegion);

            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Encountered an unexpected error while calling PayPal IPN for the region \"{CloudRegion}\"",
                cloudRegion);

            return StatusCode(500,
                $"Encountered an unexpected error while calling PayPal IPN for the region {cloudRegion}");
        }
    }

    /// <summary>
    /// Gets the cloud region from the custom fields present in the form body. If none is present, defaults to "US"
    /// </summary>
    /// <param name="formData"></param>
    /// <returns></returns>
    private static string GetCloudRegionFromCustomFields(IEnumerable<KeyValuePair<string, string>> formData)
    {
        var customField = formData.FirstOrDefault(kvp =>
            kvp.Key.Equals("custom", StringComparison.OrdinalIgnoreCase));

        if (customField.Equals(default(KeyValuePair<string, string>)))
        {
            return "US";
        }

        var customFieldsCsv = customField.Value;
        var cloudRegionRegexMatch = RegionValueCompiledRegex().Match(customFieldsCsv);
        var cloudRegion = cloudRegionRegexMatch.Success
            ? cloudRegionRegexMatch.Groups["regionValue"].Value
            : "US";

        return cloudRegion;
    }

    /// <summary>
    /// Gets the destination URL depending on the datacenter
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cloudRegion"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private string GetTargetUrl(string key, string cloudRegion)
    {
        string baseAddress;
        switch (cloudRegion)
        {
            case "EU":
                baseAddress = globalSettingOptionsSnapshot.Value.EUBillingBaseAddress.TrimEnd('/');
                break;
            default:
                {
                    // Assuming that all others are going to US
                    if (cloudRegion != "US")
                    {
                        logger.LogWarning(
                            "Expected cloud region to be either \"US\" or \"EU\", but received {CloudRegion}",
                            cloudRegion);
                    }
                    baseAddress = globalSettingOptionsSnapshot.Value.USBillingBaseAddress.TrimEnd('/');
                    break;
                }
        }

        var targetUrl = $"{baseAddress}/paypal/ipn?key={key}";
        return targetUrl;
    }

    [GeneratedRegex("region:(?<regionValue>[^,]*)")]
    private static partial Regex RegionValueCompiledRegex();
}
