using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Microsoft.Extensions.Logging;
using TracesNT.Services;

namespace TracesNT.ClientBehaviours;

public class MetricsMessageInspector(ITracesNtClientMetricsService metricsService, ILogger logger)
    : IClientMessageInspector
{
    private sealed record CorrelationState(Stopwatch Stopwatch, string Action);

    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var action = request.Headers.Action;

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Starting SOAP Request metrics for Action: {Action}", action);

        return new CorrelationState(Stopwatch.StartNew(), action);
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Ending SOAP Request metrics");

            var (stopwatch, action) = (CorrelationState)correlationState;
            stopwatch.Stop();

            int? httpStatusCode = null;
            string? soapFaultCode = null;

            if (reply.Properties.TryGetValue(HttpResponseMessageProperty.Name, out var httpResponseProperty))
            {
                httpStatusCode = (int)((HttpResponseMessageProperty)httpResponseProperty).StatusCode;
            }

            if (reply.IsFault)
            {
                var buffer = reply.CreateBufferedCopy(int.MaxValue);
                var fault = MessageFault.CreateFault(buffer.CreateMessage(), int.MaxValue);
                reply = buffer.CreateMessage();
                soapFaultCode = fault.Code.Name;
            }

            metricsService.RecordRequest(action, stopwatch.ElapsedMilliseconds, httpStatusCode, soapFaultCode);

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Published SOAP Request metrics");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish TracesNT metrics");
        }
    }
}
