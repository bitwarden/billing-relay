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
    private readonly PayPalController _payPalController;
    private const string _key = "key";
    private const string _euBillingAddress = "https://bitwarden.eu";
    private const string _usBillingAddress = "https://bitwarden.com";
    private const string _accountCredit = "1";
    private readonly string _organizationId = Guid.NewGuid().ToString();

    public PayPalControllerTests()
    {
        var logger = Substitute.For<ILogger<PayPalController>>();
        var globalSettingsOptionsSnapshot = Substitute.For<IOptionsSnapshot<GlobalSettingsOptions>>();

        var globalSettingsOptions = new GlobalSettingsOptions
        {
            EUBillingBaseAddress = _euBillingAddress,
            USBillingBaseAddress = _usBillingAddress
        };

        globalSettingsOptionsSnapshot.Value.Returns(globalSettingsOptions);

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _payPalController = new PayPalController(logger, _httpClientFactory, globalSettingsOptionsSnapshot);
    }

    private class CustomFieldsGenerator : TheoryData<(string region, string expectedBillingAddress, bool emptyCase)>
    {
        public CustomFieldsGenerator()
        {
            Add(("US", _usBillingAddress, false));
            Add(("EU", _euBillingAddress, false));
            Add(("US", _usBillingAddress, true));
        }
    }

    [Theory]
    [ClassData(typeof(CustomFieldsGenerator))]
    public async Task PostIpnAsync_CallsCorrectRegion_OK_RespondsWith200((string, string, bool) theoryData)
    {
        // Arrange
        var (region, expectedBillingAddress, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);
        var messageHandler = ConfigureResponseWith(HttpStatusCode.OK, "OK");

        // Act
        var result  = await _payPalController.PostIpnAsync(_key);

        // Assert
        result.CheckFor(StatusCodes.Status200OK);

        _httpClientFactory.Received(1).CreateClient();
        messageHandler.Invocations.Should().Be(1);

        var requestContent = await ConvertToRequestContentAsync(formData);
        messageHandler.RequestContent.Should().Be(requestContent);
        messageHandler.RequestUri.Should().Be($"{expectedBillingAddress}/paypal/ipn?key={_key}");
    }

    [Theory]
    [ClassData(typeof(CustomFieldsGenerator))]
    public async Task PostIpnAsync_CallsCorrectRegion_OtherStatusCode_RespondsWithStatusCode((string, string, bool) theoryData)
    {
        // Arrange
        var (region, expectedBillingAddress, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);

        const string responseContent = "BAD REQUEST";

        var messageHandler = ConfigureResponseWith(HttpStatusCode.BadRequest, responseContent);

        // Act
        var result  = await _payPalController.PostIpnAsync(_key);

        // Assert
        result.CheckFor(StatusCodes.Status400BadRequest, responseContent);

        _httpClientFactory.Received(1).CreateClient();
        messageHandler.Invocations.Should().Be(1);

        var requestContent = await ConvertToRequestContentAsync(formData);
        messageHandler.RequestContent.Should().Be(requestContent);
        messageHandler.RequestUri.Should().Be($"{expectedBillingAddress}/paypal/ipn?key={_key}");
    }

    [Theory]
    [ClassData(typeof(CustomFieldsGenerator))]
    public async Task PostIpnAsync_CallsCorrectRegion_Exception_RespondsWith500((string, string, bool) theoryData)
    {
        // Arrange
        var (region, _, emptyCase) = theoryData;

        var formData = GetFormData(_organizationId, _accountCredit, region, emptyCase);

        ConfigureRequestWith(formData);

        _httpClientFactory.CreateClient().Throws<Exception>();

        // Act
        var result  = await _payPalController.PostIpnAsync(_key);

        // Assert
        result.CheckFor(StatusCodes.Status500InternalServerError, $"Encountered an unexpected error while calling PayPal IPN for the region {region}");
    }

    private void ConfigureRequestWith(Dictionary<string, StringValues> data)
    {
        var context = new ControllerContext { HttpContext =
            new DefaultHttpContext {
                Request = { Form = new FormCollection(data) }
            }
        };

        _payPalController.ControllerContext = context;
    }

    private MockHttpMessageHandler ConfigureResponseWith(HttpStatusCode statusCode, string responseContent)
    {
        var messageHandler = new MockHttpMessageHandler(statusCode, responseContent);

        _httpClientFactory
            .CreateClient()
            .Returns(new HttpClient(messageHandler));

        return messageHandler;
    }

    private static async Task<string> ConvertToRequestContentAsync(IDictionary<string, StringValues> formData)
    {
        var cleaned = formData
            .Select(keyValuePair => new KeyValuePair<string, string>(keyValuePair.Key, keyValuePair.Value!));

        var formUrlEncodedContent = new FormUrlEncodedContent(cleaned);

        return await formUrlEncodedContent.ReadAsStringAsync();
    }

    private static Dictionary<string, StringValues> GetFormData(
        string organizationId,
        string accountCredit,
        string region,
        bool emptyCase)
    => emptyCase ?
        new Dictionary<string, StringValues>() :
        new Dictionary<string, StringValues> { { "custom", $"organization_id:{organizationId},account_credit:{accountCredit},region:{region}" } };
}
