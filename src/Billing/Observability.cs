using System.Diagnostics.Metrics;

namespace Billing;

public static class Observability
{
    private const string _meterName = "billing-relay";
    private const string _counterCloudRegionName = _meterName + ".cloud_region_requests.count";

    private static Meter _meter;
    private static Counter<int> _counterCloudRegion;

    static Observability()
    {
        _meter = new Meter(_meterName);
        _counterCloudRegion = _meter.CreateCounter<int>(_counterCloudRegionName);
    }

    public static void IncrementCloudRegionCounter(string cloudRegion)
    {
        _counterCloudRegion.Add(1, new KeyValuePair<string, object?>("region", cloudRegion.ToLowerInvariant()));
    }
}
