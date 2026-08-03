using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace TracesNT.ClientBehaviours;

public class WsSecurityMessageInspector(TracesNtCredentials credentials) : IClientMessageInspector
{
    private const string WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    public object? BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        if (string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.AuthenticationKey))
        {
            return null;
        }

        // Remove the empty Security header added by the generated client
        var headerIndex = request.Headers.FindHeader("Security", WsseNamespace);
        if (headerIndex >= 0)
        {
            request.Headers.RemoveAt(headerIndex);
        }

        request.Headers.Add(new WsSecurityHeader(credentials.Username, credentials.AuthenticationKey));

        return null;
    }

    public void AfterReceiveReply(ref Message reply, object correlationState) { }
}
