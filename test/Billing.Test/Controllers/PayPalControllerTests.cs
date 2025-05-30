using System.Net;
using Billing.Controllers;
using Billing.Options;
using Billing.Test.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Billing.Test.Controllers;

public class PayPalControllerTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayPalController> _logger;
    private readonly IOptionsSnapshot<GlobalSettingsOptions> _globalSettingsOptionsSnapshot;
    private readonly PayPalController _payPalController;

    private const string _webhookKey = "test-webhook-key";
    private const string _usWebhookKey = "us-downstream-key";
    private const string _euWebhookKey = "eu-downstream-key";
    private const string _usBillingAddress = "https://bitwarden.com";
    private const string _euBillingAddress = "https://bitwarden.eu";
    private const string _accountCredit = "1";
    private readonly string _organizationId = Guid.NewGuid().ToString();

    public PayPalControllerTests()
    {
        _logger = Substitute.For<ILogger<PayPalController>>();
        _globalSettingsOptionsSnapshot = Substitute.For<IOptionsSnapshot<GlobalSettingsOptions>>();

        var globalSettingsOptions = new GlobalSettingsOptions
        {
            WebhookKey = _webhookKey,
            Environments = new Dictionary<string, EnvironmentConfig>
            {
                ["US"] = new EnvironmentConfig
                {
                    BaseAddress = _usBillingAddress,
                    WebhookKey = _usWebhookKey
                },
                ["EU"] = new EnvironmentConfig
                {
                    BaseAddress = _euBillingAddress,
                    WebhookKey = _euWebhookKey
                }
            }
        };

        _globalSettingsOptionsSnapshot.Value.Returns(globalSettingsOptions);

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _payPalController = new PayPalController(_logger, _httpClientFactory, _globalSettingsOptionsSnapshot);
    }

    [Fact]
    public async Task PostIpnAsync_InvalidWebhookKey_ReturnsUnauthorized()
    {
        // Arrange
        const string invalidKey = "invalid-key";
        var formData = GetFormData(_organizationId, _accountCredit, "US", false);
        ConfigureRequestWith(formData);

        // Act
        var result = await _payPalController.PostIpnAsync(invalidKey);

        // Assert
        result.CheckFor(StatusCodes.Status401Unauthorized, "Invalid webhook key");
        _logger.Received(1).LogWarning("Invalid webhook key provided");
    }

    [Fact]
    public async Task PostIpnAsync_EmptyWebhookKey_ReturnsUnauthorized()
    {
        // Arrange
        var formData = GetFormData(_organizationId, _accountCredit, "US", false);
        ConfigureRequestWith(formData);

        // Act
        var result = await _payPalController.PostIpnAsync(string.Empty);

        // Assert
        result.CheckFor(StatusCodes.Status401Unauthorized, "Invalid webhook key");
    }

    [Fact]
    public async Task PostIpnAsync_NullWebhookKey_ReturnsUnauthorized()
    {
        // Arrange
        var formData = GetFormData(_organizationId, _accountCredit, "US", false);
        ConfigureRequestWith(formData);

        // Act
        var result = await _payPalController.PostIpnAsync(null!);

        // Assert
        result.CheckFor(StatusCodes.Status401Unauthorized, "Invalid webhook key");
    }

    private class EnvironmentTestDataGenerator : TheoryData<(string region, string expectedBillingAddress, string expectedWebhookKey, bool emptyCase)>
    {
        public EnvironmentTestDataGenerator()
        {
            Add(("US", _usBillingAddress, _usWebhookKey, false));
            Add(("EU", _euBillingAddress, _euWebhookKey, false));
            Add(("us", _usBillingAddress, _usWebhookKey, false)); // Test case insensitive
            Add(("eu", _euBillingAddress, _euWebhookKey, false)); // Test case insensitive
            Add(("US", _usBillingAddress, _usWebhookKey, true)); // Empty form data defaults to US
        }
    }

    [Theory]
    [ClassData(typeof(EnvironmentTestDataGenerator))]
    public async Task PostIpnAsync_ValidWebhookKey_CallsCorrectRegion_OK_RespondsWith200((string, string, string, bool) theoryData)
    {
        // Arrange
        var (region, expectedBillingAddress, expectedWebhookKey, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);
        var messageHandler = ConfigureResponseWith(HttpStatusCode.OK, "OK");

        // Act
        var result = await _payPalController.PostIpnAsync(_webhookKey);

        // Assert
        result.CheckFor(StatusCodes.Status200OK);

        _httpClientFactory.Received(1).CreateClient();
        messageHandler.Invocations.Should().Be(1);

        var requestContent = await ConvertToRequestContentAsync(formData);
        messageHandler.RequestContent.Should().Be(requestContent);
        messageHandler.RequestUri.Should().Be($"{expectedBillingAddress}/paypal/ipn?key={expectedWebhookKey}");
    }

    [Theory]
    [ClassData(typeof(EnvironmentTestDataGenerator))]
    public async Task PostIpnAsync_ValidWebhookKey_CallsCorrectRegion_OtherStatusCode_RespondsWithStatusCode((string, string, string, bool) theoryData)
    {
        // Arrange
        var (region, expectedBillingAddress, expectedWebhookKey, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);

        const string responseContent = "BAD REQUEST";
        var messageHandler = ConfigureResponseWith(HttpStatusCode.BadRequest, responseContent);

        // Act
        var result = await _payPalController.PostIpnAsync(_webhookKey);

        // Assert
        result.CheckFor(StatusCodes.Status400BadRequest, responseContent);

        _httpClientFactory.Received(1).CreateClient();
        messageHandler.Invocations.Should().Be(1);

        var requestContent = await ConvertToRequestContentAsync(formData);
        messageHandler.RequestContent.Should().Be(requestContent);
        messageHandler.RequestUri.Should().Be($"{expectedBillingAddress}/paypal/ipn?key={expectedWebhookKey}");
    }

    [Theory]
    [ClassData(typeof(EnvironmentTestDataGenerator))]
    public async Task PostIpnAsync_ValidWebhookKey_CallsCorrectRegion_Exception_RespondsWith500((string, string, string, bool) theoryData)
    {
        // Arrange
        var (region, _, _, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);

        _httpClientFactory.CreateClient().Throws<Exception>();

        // Expected region should be the parsed region or "US" if empty/invalid
        var expectedRegion = emptyCase ? "US" : region;

        // Act
        var result = await _payPalController.PostIpnAsync(_webhookKey);

        // Assert
        result.CheckFor(StatusCodes.Status500InternalServerError, $"Encountered an unexpected error while calling PayPal IPN for the region {expectedRegion}");
    }

    [Theory]
    [ClassData(typeof(EnvironmentTestDataGenerator))]
    public async Task PostIpnAsync_ValidWebhookKey_PreservesFormDataOrder((string, string, string, bool) theoryData)
    {
        // Arrange
        var (region, _, _, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);
        var messageHandler = ConfigureResponseWith(HttpStatusCode.OK, "OK");

        // Act
        _ = await _payPalController.PostIpnAsync(_webhookKey);

        // Assert
        _httpClientFactory.Received(1).CreateClient();
        messageHandler.Invocations.Should().Be(1);

        var requestContent = await ConvertToRequestContentAsync(formData);
        messageHandler.RequestContent.Should().Be(requestContent);

        var sentKeyValuePairs = ParseFormUrlEncodedPairs(messageHandler.RequestContent ?? string.Empty);
        sentKeyValuePairs.Should().BeEquivalentTo(formData, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task PostIpnAsync_UnknownRegion_DefaultsToUS()
    {
        // Arrange
        var formData = GetFormData(_organizationId, _accountCredit, "UNKNOWN", false);
        ConfigureRequestWith(formData);
        var messageHandler = ConfigureResponseWith(HttpStatusCode.OK, "OK");

        // Act
        var result = await _payPalController.PostIpnAsync(_webhookKey);

        // Assert
        result.CheckFor(StatusCodes.Status200OK);

        messageHandler.RequestUri.Should().Be($"{_usBillingAddress}/paypal/ipn?key={_usWebhookKey}");
    }

    [Fact]
    public async Task PostIpnAsync_NoUSEnvironmentConfigured_ThrowsException()
    {
        // Arrange
        var globalSettingsOptions = new GlobalSettingsOptions
        {
            WebhookKey = _webhookKey,
            Environments = new Dictionary<string, EnvironmentConfig>
            {
                ["EU"] = new EnvironmentConfig
                {
                    BaseAddress = _euBillingAddress,
                    WebhookKey = _euWebhookKey
                }
            }
        };

        _globalSettingsOptionsSnapshot.Value.Returns(globalSettingsOptions);

        var controller = new PayPalController(_logger, _httpClientFactory, _globalSettingsOptionsSnapshot);
        var formData = GetFormData(_organizationId, _accountCredit, "UNKNOWN", false);

        var context = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Form = new FormCollection(formData.ToDictionary(
                        pair => pair.Key,
                        pair => new StringValues(pair.Value)))
                }
            }
        };

        controller.ControllerContext = context;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.PostIpnAsync(_webhookKey));

        exception.Message.Should().Be("No US environment configured and no matching region found");
    }

    [Fact]
    public async Task PostIpnAsync_EmptyConfiguredWebhookKey_LogsError()
    {
        // Arrange
        var globalSettingsOptions = new GlobalSettingsOptions
        {
            WebhookKey = string.Empty,
            Environments = new Dictionary<string, EnvironmentConfig>
            {
                ["US"] = new EnvironmentConfig
                {
                    BaseAddress = _usBillingAddress,
                    WebhookKey = _usWebhookKey
                }
            }
        };

        _globalSettingsOptionsSnapshot.Value.Returns(globalSettingsOptions);

        var controller = new PayPalController(_logger, _httpClientFactory, _globalSettingsOptionsSnapshot);
        var formData = GetFormData(_organizationId, _accountCredit, "US", false);
        ConfigureRequestWith(formData, controller);

        // Act
        var result = await controller.PostIpnAsync("some-key");

        // Assert
        result.CheckFor(StatusCodes.Status401Unauthorized, "Invalid webhook key");
        _logger.Received(1).LogError("No webhook key configured in settings");
    }

    private void ConfigureRequestWith(List<KeyValuePair<string, string>> data, PayPalController? controller = null)
    {
        var targetController = controller ?? _payPalController;

        var context = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Form = new FormCollection(data.ToDictionary(
                        pair => pair.Key,
                        pair => new StringValues(pair.Value)))
                }
            }
        };

        targetController.ControllerContext = context;
    }

    private MockHttpMessageHandler ConfigureResponseWith(HttpStatusCode statusCode, string responseContent)
    {
        var messageHandler = new MockHttpMessageHandler(statusCode, responseContent);

        _httpClientFactory
            .CreateClient()
            .Returns(new HttpClient(messageHandler));

        return messageHandler;
    }

    private static async Task<string> ConvertToRequestContentAsync(List<KeyValuePair<string, string>> formData)
    {
        var formUrlEncodedContent = new FormUrlEncodedContent(formData);

        return await formUrlEncodedContent.ReadAsStringAsync();
    }

    private static List<KeyValuePair<string, string>> GetFormData(
        string organizationId,
        string accountCredit,
        string region,
        bool emptyCase)
    => emptyCase ?
        new List<KeyValuePair<string, string>>() :
        new List<KeyValuePair<string, string>> { new("custom", $"organization_id:{organizationId},account_credit:{accountCredit},region:{region}") };

    private static IEnumerable<KeyValuePair<string, string>> ParseFormUrlEncodedPairs(string content) =>
        content
            .Split('&')
            .Where(pair => pair.Contains('='))
            .Select(pair =>
            {
                var splitPair = pair.Split('=');
                return new KeyValuePair<string, string>(
                    WebUtility.UrlDecode(splitPair[0]),
                    WebUtility.UrlDecode(splitPair[1]));
            })
            .ToList();
}
