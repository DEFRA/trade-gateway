using System.Text.Json;
using Api.Models.Unece;
using Xunit;

namespace JsonSchemaToCSharp.Tests;

public class RoundTripTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void Deserialize_UnvtdIntraExample_MapsTopLevelFields()
    {
        var json = File.ReadAllText("unvtd-intra.json");
        var model = JsonSerializer.Deserialize<CertificatePayload>(json, SerializerOptions)!;

        Assert.Equal("defra/certificate-internal/1", model.Model);
        Assert.Equal("intra", model.Type);
    }

    [Fact]
    public void Deserialize_UnvtdIntraExample_MapsExchangedDocument()
    {
        var json = File.ReadAllText("unvtd-intra.json");
        var doc = JsonSerializer.Deserialize<CertificatePayload>(json, SerializerOptions)!.ExchangedDocument;

        Assert.Equal("INTRA.EU.NL.2021.0000001", doc.Identifier);
        Assert.Equal("856", doc.DocumentTypeCode);
        Assert.Equal("70", doc.DocumentStatusCode);
        Assert.Equal("2021-02-18T16:09:51.000+01:00", doc.IssueDateTime);

        Assert.Equal(3, doc.IncludedNote?.Count);
        Assert.Equal("LAST_UPDATE_DATETIME", doc.IncludedNote![0].NoteSubjectCode);

        Assert.Equal(2, doc.ReferenceDocument?.Count);
        Assert.Equal("CAW", doc.ReferenceDocument![0].RelationshipTypeCode);
        Assert.Equal("BM", doc.ReferenceDocument[1].RelationshipTypeCode);
    }

    [Fact]
    public void Deserialize_UnvtdIntraExample_MapsSignatories()
    {
        var json = File.ReadAllText("unvtd-intra.json");
        var doc = JsonSerializer.Deserialize<CertificatePayload>(json, SerializerOptions)!.ExchangedDocument;

        var first = doc.FirstSignatoryAuthentication!;
        Assert.Equal("4", first.GovernmentActionTypeCode);
        Assert.Equal("2021-02-18T16:10:29.000+01:00", first.ActualDateTime);
        Assert.NotNull(first.ProviderParty);
        Assert.Equal("Agropoli Vet Office", first.ProviderParty!.Name);
        Assert.Equal("VJ", first.ProviderParty.PartyRoleCode);

        var second = doc.SecondSignatoryAuthentication!;
        Assert.Equal("1", second.GovernmentActionTypeCode);

        var third = doc.ThirdSignatoryAuthentication!;
        Assert.Equal("8", third.GovernmentActionTypeCode);
        Assert.Equal(2, third.IncludedClause?.Count);
    }

    [Fact]
    public void Deserialize_UnvtdIntraExample_MapsConsignment()
    {
        var json = File.ReadAllText("unvtd-intra.json");
        var consignment = JsonSerializer.Deserialize<CertificatePayload>(json, SerializerOptions)!.SpecifiedConsignment[0];

        Assert.Equal("2021-02-20T16:07:00.000+01:00", consignment.AvailabilityDueDateTime);

        var consignor = consignment.ConsignorParty!;
        Assert.Equal("B&S GTC Customs B.V.", consignor.Name);
        Assert.Equal("CZ", consignor.PartyRoleCode);

        // Unknown schema fields are preserved in ExtensionData
        Assert.NotNull(consignment.ExtensionData);
        Assert.True(consignment.ExtensionData!.ContainsKey("mainCarriageLogisticsTransportMovement"),
            "mainCarriageLogisticsTransportMovement should be captured in ExtensionData (missing from schema)");
        Assert.True(consignment.ExtensionData.ContainsKey("utilizedLogisticsTransportEquipment"),
            "utilizedLogisticsTransportEquipment should be captured in ExtensionData (missing from schema)");
    }

    [Fact]
    public void Deserialize_UnvtdIntraExample_RootExtensionDataCapturesContextFields()
    {
        var json = File.ReadAllText("unvtd-intra.json");
        var model = JsonSerializer.Deserialize<CertificatePayload>(json, SerializerOptions)!;

        Assert.NotNull(model.ExtensionData);
        Assert.True(model.ExtensionData!.ContainsKey("$schema"), "$schema should be captured in ExtensionData");
        Assert.True(model.ExtensionData.ContainsKey("@context"), "@context should be captured in ExtensionData");
    }

    [Fact]
    public void RoundTrip_UnvtdIntraExample_PreservesAllData()
    {
        var originalJson = File.ReadAllText("unvtd-intra.json");
        var model = JsonSerializer.Deserialize<CertificatePayload>(originalJson, SerializerOptions)!;
        var reserializedJson = JsonSerializer.Serialize(model, SerializerOptions);

        using var originalDoc = JsonDocument.Parse(originalJson);
        using var reserializedDoc = JsonDocument.Parse(reserializedJson);

        Assert.True(
            JsonElementDeepEquals(originalDoc.RootElement, reserializedDoc.RootElement),
            $"Round-trip mismatch.\nOriginal: {originalJson}\n\nReserialized: {reserializedJson}");
    }

    private static bool JsonElementDeepEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;

        return a.ValueKind switch
        {
            JsonValueKind.Object => JsonObjectsEqual(a, b),
            JsonValueKind.Array => JsonArraysEqual(a, b),
            JsonValueKind.String => a.GetString() == b.GetString(),
            JsonValueKind.Number => a.GetRawText() == b.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => true,
            JsonValueKind.Null => true,
            _ => false,
        };
    }

    private static bool JsonObjectsEqual(JsonElement a, JsonElement b)
    {
        var aProps = a.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var bProps = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        if (aProps.Count != bProps.Count) return false;

        foreach (var (key, aValue) in aProps)
        {
            if (!bProps.TryGetValue(key, out var bValue)) return false;
            if (!JsonElementDeepEquals(aValue, bValue)) return false;
        }

        return true;
    }

    private static bool JsonArraysEqual(JsonElement a, JsonElement b)
    {
        var aArr = a.EnumerateArray().ToList();
        var bArr = b.EnumerateArray().ToList();
        if (aArr.Count != bArr.Count) return false;
        return aArr.Zip(bArr).All(pair => JsonElementDeepEquals(pair.First, pair.Second));
    }
}
