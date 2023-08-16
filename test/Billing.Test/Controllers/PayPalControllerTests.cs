using Billing.Controllers;
using Billing.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Billing.Test.Controllers;

public class PayPalControllerTests
{
    private PayPalController CreateSut()
    {
        var logger = Substitute.For<ILogger<PayPalController>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var globalSettingOptionsSnapshot = Substitute.For<IOptionsSnapshot<GlobalSettingsOptions>>();

        var sut = new PayPalController(logger, httpClientFactory, globalSettingOptionsSnapshot);

        return sut;
    }

    [Fact]
    public void Placeholder()
    {
        var sut = CreateSut();

        // actually do something

        Assert.Equal(1, 1);
    }
}
