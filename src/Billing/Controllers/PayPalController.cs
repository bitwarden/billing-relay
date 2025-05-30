using System.Security.Cryptography;
using System.Text;
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
    IOptions<GlobalSettingsOptions> globalSettingOptionsSnapshot)
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

        if (!IsValidWebhookKey(key))
        {
            logger.LogWarning("Invalid webhook key provided");
            return Unauthorized("Invalid webhook key");
        }

        var formData = Request.Form
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()))
            .ToList();

        var cloudRegion = GetCloudRegionFromCustomFields(formData);
        var targetUrl = GetTargetUrl(cloudRegion);
        var formContent = new FormUrlEncodedContent(formData);

        try
        {
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
    /// Validates the webhook key using secure string comparison
    /// </summary>
    /// <param name="providedKey"></param>
    /// <returns></returns>
    private bool IsValidWebhookKey(string providedKey)
    {
        if (string.IsNullOrEmpty(providedKey))
        {
            return false;
        }

        var configuredKey = globalSettingOptionsSnapshot.Value.WebhookKey;
        if (string.IsNullOrEmpty(configuredKey))
        {
            logger.LogError("No webhook key configured in settings");
            return false;
        }

        var providedKeyBytes = Encoding.UTF8.GetBytes(providedKey);
        var configuredKeyBytes = Encoding.UTF8.GetBytes(configuredKey);

        return CryptographicOperations.FixedTimeEquals(providedKeyBytes, configuredKeyBytes);
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

        return cloudRegionRegexMatch.Success
            ? cloudRegionRegexMatch.Groups["regionValue"].Value
            : "US";
    }

    /// <summary>
    /// Gets the destination URL depending on the datacenter
    /// </summary>
    /// <param name="cloudRegion"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private string GetTargetUrl(string cloudRegion)
    {
        EnvironmentConfig environmentConfig;

        var environments = globalSettingOptionsSnapshot.Value.Environments;
        var matchingKey = environments.Keys
            .FirstOrDefault(k => string.Equals(k, cloudRegion, StringComparison.OrdinalIgnoreCase));

        if (matchingKey != null)
        {
            environmentConfig = environments[matchingKey];
        }
        else
        {
            // Default to US if the region is not found
            logger.LogWarning(
                "Cloud region \"{CloudRegion}\" not found in configuration, defaulting to US",
                cloudRegion);

            var defaultKey = environments.Keys.FirstOrDefault(k => string.Equals(k, "US", StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException("No US environment configured and no matching region found");

            environmentConfig = environments[defaultKey];
        }

        var baseAddress = environmentConfig.BaseAddress.TrimEnd('/');
        var webhookKey = environmentConfig.WebhookKey;

        return $"{baseAddress}/paypal/ipn?key={webhookKey}";
    }

    [GeneratedRegex("region:(?<regionValue>[^,]*)")]
    private static partial Regex RegionValueCompiledRegex();
}
