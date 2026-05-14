using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace TracesNT.ClientBehaviours;

public class WsSecurityHeader(string username, string authenticationKey) : MessageHeader
{
    private const string WsseNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNamespace =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string PasswordDigestType =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";
    private const string Base64BinaryType =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

    public override string Name => "Security";
    public override string Namespace => WsseNamespace;

    protected override void OnWriteHeaderContents(
        XmlDictionaryWriter writer,
        MessageVersion messageVersion
    )
    {
        var now = DateTime.UtcNow;
        var created = now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var expires = now.AddMinutes(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var nonce = RandomNumberGenerator.GetBytes(16);
        var passwordDigest = ComputePasswordDigest(nonce, created, authenticationKey);

        WriteUsernameToken(writer, created, nonce, passwordDigest);
        WriteTimestamp(writer, created, expires);
    }

    private void WriteUsernameToken(
        XmlDictionaryWriter writer,
        string created,
        byte[] nonce,
        string passwordDigest
    )
    {
        writer.WriteStartElement("wsse", "UsernameToken", WsseNamespace);
        writer.WriteAttributeString(
            "wsu",
            "Id",
            WsuNamespace,
            Guid.NewGuid().ToString("N").ToUpperInvariant()
        );

        writer.WriteElementString("wsse", "Username", WsseNamespace, username);

        writer.WriteStartElement("wsse", "Password", WsseNamespace);
        writer.WriteAttributeString("Type", PasswordDigestType);
        writer.WriteString(passwordDigest);
        writer.WriteEndElement();

        writer.WriteStartElement("wsse", "Nonce", WsseNamespace);
        writer.WriteAttributeString("EncodingType", Base64BinaryType);
        writer.WriteString(Convert.ToBase64String(nonce));
        writer.WriteEndElement();

        writer.WriteElementString("wsu", "Created", WsuNamespace, created);

        writer.WriteEndElement();
    }

    private static void WriteTimestamp(XmlDictionaryWriter writer, string created, string expires)
    {
        writer.WriteStartElement("wsu", "Timestamp", WsuNamespace);
        writer.WriteAttributeString(
            "wsu",
            "Id",
            WsuNamespace,
            $"TS-{Guid.NewGuid().ToString("N").ToUpperInvariant()}"
        );
        writer.WriteElementString("wsu", "Created", WsuNamespace, created);
        writer.WriteElementString("wsu", "Expires", WsuNamespace, expires);
        writer.WriteEndElement();
    }

    /// <summary>
    /// WS-Security UsernameToken PasswordDigest:
    /// Base64(SHA-1(nonce + created + authentication_key))
    /// </summary>
    private static string ComputePasswordDigest(byte[] nonce, string created, string authKey)
    {
        byte[] combined =
        [
            .. nonce,
            .. Encoding.UTF8.GetBytes(created),
            .. Encoding.UTF8.GetBytes(authKey),
        ];
        return Convert.ToBase64String(SHA1.HashData(combined));
    }
}