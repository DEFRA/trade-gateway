using System.ServiceModel.Channels;
using System.Globalization;
using System.Xml.Linq;
using TracesNT.ClientBehaviours;

namespace TracesNT.Tests.ClientBehaviours;

public class WsSecurityHeaderTests
{
    private static readonly XNamespace WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace WsuNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

    [Fact]
    public void OnWriteHeaderContents_WritesExpectedWsSecurityElements()
    {
        var message = Message.CreateMessage(MessageVersion.Soap11, "urn:test-action");
        message.Headers.Add(new WsSecurityHeader("alice", "super-secret"));
        var security = XDocument.Parse(WcfTestHelpers.ReadHeaderXml(message, "Security", WsseNamespace.NamespaceName)).Root!;

        security.Name.Should().Be(WsseNamespace + "Security");

        var usernameToken = security.Element(WsseNamespace + "UsernameToken");
        usernameToken.Should().NotBeNull();
        usernameToken!.Attribute(WsuNamespace + "Id")!.Value.Should().NotBeNullOrWhiteSpace();
        usernameToken.Element(WsseNamespace + "Username")!.Value.Should().Be("alice");

        var password = usernameToken.Element(WsseNamespace + "Password");
        password.Should().NotBeNull();
        password!.Attribute("Type")!.Value.Should().Be(
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"
        );
        password.Value.Should().NotBeNullOrWhiteSpace();
        password.Value.Should().NotBe("super-secret");

        var nonce = usernameToken.Element(WsseNamespace + "Nonce");
        nonce.Should().NotBeNull();
        nonce!.Attribute("EncodingType")!.Value.Should().Be(
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"
        );
        Convert.FromBase64String(nonce.Value).Should().HaveCount(16);

        var tokenCreated = DateTimeOffset.ParseExact(
            usernameToken.Element(WsuNamespace + "Created")!.Value,
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
        );
        var timestamp = security.Element(WsuNamespace + "Timestamp");
        timestamp.Should().NotBeNull();
        timestamp!.Attribute(WsuNamespace + "Id")!.Value.Should().StartWith("TS-");

        var timestampCreated = DateTimeOffset.ParseExact(
            timestamp.Element(WsuNamespace + "Created")!.Value,
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
        );
        var timestampExpires = DateTimeOffset.ParseExact(
            timestamp.Element(WsuNamespace + "Expires")!.Value,
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
        );

        timestampCreated.Should().Be(tokenCreated);
        (timestampExpires - timestampCreated).Should().Be(TimeSpan.FromMinutes(2));
    }
}
