using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Microsoft.Extensions.Logging;

namespace TracesNT.ClientBehaviours;

public class LoggingMessageInspector(ILogger logger) : IClientMessageInspector
{
    private sealed record CorrelationState(Stopwatch Stopwatch, string Action);

    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var action = request.Headers.Action;
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("SOAP Request: Action={Action}", action);
        return new CorrelationState(Stopwatch.StartNew(), action);
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        var (stopwatch, action) = (CorrelationState)correlationState;
        stopwatch.Stop();

        if (reply.IsFault)
        {
            var buffer = reply.CreateBufferedCopy(int.MaxValue);
            var fault = MessageFault.CreateFault(buffer.CreateMessage(), int.MaxValue);
            reply = buffer.CreateMessage();
            logger.LogError(
                "Service Fault: Action={Action}, Duration={Duration}ms, Code={Code}, Reason={Reason}",
                action,
                stopwatch.ElapsedMilliseconds,
                fault.Code.Name,
                fault.Reason
            );
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Service Response: Action={Action}, Duration={Duration}ms",
                    action,
                    stopwatch.ElapsedMilliseconds
                );
        }
    }
}
