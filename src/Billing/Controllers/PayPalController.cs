using System.Text.RegularExpressions;
using Billing.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Billing.Controllers;

[ApiController]
[Route("paypal")]
public partial class PayPalController : ControllerBase
{
    private readonly ILogger<PayPalController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsSnapshot<GlobalSettingsOptions> _globalSettingOptionsSnapshot;

    public PayPalController(
        ILogger<PayPalController> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<GlobalSettingsOptions> globalSettingOptionsSnapshot)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _globalSettingOptionsSnapshot = globalSettingOptionsSnapshot;
    }

    /// <summary>
    /// Handles the logic for routing PayPal IPN traffic depending on the datacenter
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost("ipn")]
    public async Task<IActionResult> PostIpnAsync([FromQuery] string key)
    {
        _logger.LogDebug("Mothership: PayPal IPN has been hit");
        var formData = Request.Form
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        var cloudRegion = GetCloudRegionFromCustomFields(formData);
        var targetUrl = GetTargetUrl(key, cloudRegion);

        var formContent = new FormUrlEncodedContent(formData);

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(targetUrl, formContent);

        return StatusCode((int)response.StatusCode);
    }

    /// <summary>
    /// Gets the cloud region from the custom fields present in the form body. If none is present, defaults to "US"
    /// </summary>
    /// <param name="formData"></param>
    /// <returns></returns>
    private static string GetCloudRegionFromCustomFields(Dictionary<string, string> formData)
    {
        if (!formData.ContainsKey("custom"))
        {
            return "US";
        }
        
        var customFieldsCsv = formData["custom"];
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
        var baseAddress = cloudRegion switch
        {
            "US" => _globalSettingOptionsSnapshot.Value
                .USBillingBaseAddress
                .TrimEnd('/'),
            "EU" => _globalSettingOptionsSnapshot.Value
                .EUBillingBaseAddress
                .TrimEnd('/'),
            _ => throw new Exception("Invalid datacenter detected")
        };

        var targetUrl = $"{baseAddress}/paypal/ipn?key={key}";
        return targetUrl;
    }

    [GeneratedRegex("region:(?<regionValue>[^,]*)")]
    private static partial Regex RegionValueCompiledRegex();
}