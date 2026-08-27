using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TracesNT.Services;

public class TracesNtClientMetricsService : ITracesNtClientMetricsService
{
    public const string MeterName = "Defra.TradeGateway.Api";

    private readonly Histogram<long> _requestDuration;

    public TracesNtClientMetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _requestDuration = meter.CreateHistogram<long>("RequestDuration", "MILLISECONDS", "TracesNT request duration");
    }

    public void RecordRequest(string action, long requestDuration, int? httpStatusCode, string? soapFaultCode)
    {
        var tags = new TagList
        {
            { Constants.Tags.Service, Process.GetCurrentProcess().ProcessName },
            { Constants.Tags.Action, action },
            { Constants.Tags.ResponseStatusCode, httpStatusCode },
            { Constants.Tags.FaultCode, soapFaultCode },
        };

        _requestDuration.Record(requestDuration, tags);
    }

    private static class Constants
    {
        public static class Tags
        {
            public const string Service = "ServiceName";
            public const string Action = "Action";
            public const string ResponseStatusCode = "ResponseStatusCode";
            public const string FaultCode = "FaultCode";
        }
    }
}
