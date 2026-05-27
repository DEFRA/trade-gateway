using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TracesNT.Tests.ClientBehaviours;

internal static class WcfTestHelpers
{
    [ServiceContract]
    internal interface ITestContract
    {
        [OperationContract]
        string Ping();
    }

    internal static ServiceEndpoint CreateEndpoint() =>
        new(
            ContractDescription.GetContract(typeof(ITestContract)),
            new BasicHttpBinding(),
            new EndpointAddress("http://localhost/test")
        );

    internal static ClientRuntime CreateClientRuntime()
    {
        var ctor = typeof(ClientRuntime).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(string)],
            modifiers: null
        );

        ctor.Should().NotBeNull();
        return (ClientRuntime)ctor!.Invoke(["test-endpoint", "test-contract"]);
    }

    internal static int CountHeaders(Message message, string name, string ns)
    {
        var count = 0;
        for (var index = 0; index < message.Headers.Count; index++)
        {
            if (message.Headers[index].Name == name && message.Headers[index].Namespace == ns)
            {
                count++;
            }
        }

        return count;
    }

    internal static string ReadHeaderXml(Message message, string name, string ns)
    {
        var index = message.Headers.FindHeader(name, ns);
        index.Should().BeGreaterThanOrEqualTo(0);

        using var reader = message.Headers.GetReaderAtHeader(index);
        return reader.ReadOuterXml();
    }
}
