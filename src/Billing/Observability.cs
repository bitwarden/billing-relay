using System.Diagnostics.Metrics;

namespace Billing;

public class Observability
{
    private const string _meterName = "billing-relay";
    private const string _counterCloudRegionName = _meterName + ".cloud_region_requests.count";

    private Counter<int> _counterCloudRegion;

    public Observability(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(_meterName);
        _counterCloudRegion = meter.CreateCounter<int>(_counterCloudRegionName);
    }

    public void TrackCloudRegion(string cloudRegion)
    {
        _counterCloudRegion.Add(1, new KeyValuePair<string, object?>("region", cloudRegion.ToLowerInvariant()));
    }
}
