namespace TracesNT.Services;

public interface ITracesNtClientMetricsService
{
    void RecordRequest(string action, long requestDuration, int? httpStatusCode, string? soapFaultCode);
}
