using System.Diagnostics.Metrics;

namespace Billing;

public sealed class Observability : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<int> _counterCloudRegion;

    public Observability(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Bitwarden.BillingRelay");
        _counterCloudRegion = _meter.CreateCounter<int>(
            "bitwarden.billing_relay.paypal_requests",
            unit: "{request}",
            description: "Number of requests relayed for PayPal.");
    }


    public void TrackPayPalRequest(string cloudRegion)
    {
        _counterCloudRegion.Add(1, new KeyValuePair<string, object?>("region", cloudRegion.ToLowerInvariant()));
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
