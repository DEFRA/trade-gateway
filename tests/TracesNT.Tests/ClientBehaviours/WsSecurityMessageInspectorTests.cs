using System.ServiceModel;
using System.ServiceModel.Channels;
using NSubstitute;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class WsSecurityMessageInspectorTests
{
    private const string WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private readonly IClientChannel _channel = Substitute.For<IClientChannel>();

    [Fact]
    public void BeforeSendRequest_WithCredentials_ReplacesExistingSecurityHeader()
    {
        var sut = new WsSecurityMessageInspector(
            new TracesNtConfig { Username = "alice", AuthenticationKey = "secret" }
        );
        var request = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");
        request.Headers.Add(MessageHeader.CreateHeader("Security", WsseNamespace, string.Empty));

        var result = sut.BeforeSendRequest(ref request, _channel);

        result.Should().BeNull();
        WcfTestHelpers.CountHeaders(request, "Security", WsseNamespace).Should().Be(1);
        WcfTestHelpers.ReadHeaderXml(request, "Security", WsseNamespace)
            .Should()
            .Contain("UsernameToken")
            .And.Contain("alice");
    }

    [Fact]
    public void BeforeSendRequest_WithoutCredentials_LeavesHeadersUntouched()
    {
        var sut = new WsSecurityMessageInspector(
            new TracesNtConfig { Username = "", AuthenticationKey = "secret" }
        );
        var request = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");
        request.Headers.Add(MessageHeader.CreateHeader("Security", WsseNamespace, string.Empty));
        var originalHeader = WcfTestHelpers.ReadHeaderXml(request, "Security", WsseNamespace);

        var result = sut.BeforeSendRequest(ref request, _channel);

        result.Should().BeNull();
        WcfTestHelpers.CountHeaders(request, "Security", WsseNamespace).Should().Be(1);
        WcfTestHelpers.ReadHeaderXml(request, "Security", WsseNamespace).Should().Be(originalHeader);
    }
}
