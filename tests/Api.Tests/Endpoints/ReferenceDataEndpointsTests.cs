using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Text.Json;
using Api.Constants;
using Api.Contract;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ReferenceDataEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    [Fact]
    public async Task GetClassificationSections_ReturnsMappedResponse()
    {
        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationSections\"",
                    "/*[local-name() = 'GetClassificationSectionsRequest']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml"
                        )
                )
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync(
            "/classificationSections",
            TestContext.Current.CancellationToken
        );
        var payload =
            await response.Content.ReadFromJsonAsync<DefraUNVTDProfileClassificationSectionListResponse>(
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>(),
            response.Content.Headers.ContentType?.MediaType
        );
        Assert.NotNull(payload);
        Assert.Equal(ReferenceDataService.ReferenceDataServiceV1, payload.Service);
        Assert.Contains(
            payload.Sections!,
            section =>
                section.ClassCode == "0101"
                && section.Chapter == "01"
                && section.Lms
                && section.Description == "Live horses"
                && section.Active == true
                && section.Scopes.SequenceEqual(["GB", "XI"])
        );
    }

    [Fact]
    public async Task GetClassificationTree_ReturnsMappedResponse()
    {
        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationTree\"",
                    "/*[local-name() = 'GetClassificationTreeRequest']/*[local-name() = 'TreeID' and text() = 'intra_trade']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationTreeResponse_INTRA_TRADE.xml"
                        )
                )
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync(
            "/classificationTrees/intra_trade",
            TestContext.Current.CancellationToken
        );
        var payload =
            await response.Content.ReadFromJsonAsync<DefraUNVTDProfileClassificationTreeResponse>(
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>(),
            response.Content.Headers.ContentType?.MediaType
        );
        Assert.NotNull(payload);
        Assert.Equal("intra_trade", payload.TreeId);
        Assert.NotNull(payload.Nodes);
        Assert.Equal(6, payload.Nodes.Count);
        Assert.Equal("R/N-10000", payload.Nodes[0].Path);
        Assert.Equal("LIVE ANIMALS", payload.Nodes[0].Label);
        Assert.Equal("nomenclature", payload.Nodes[0].NodeType);
        Assert.False(payload.Nodes[0].Selectable);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_ReturnsMappedResponse()
    {
        const string nodePath = "R/N-10000/N-10065/L-10121/L-10301/C-11978";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationTreeNodeDetail\"",
                    $"/*[local-name() = 'GetClassificationTreeNodeDetailRequest'][*[local-name() = 'TreeID' and text() = 'intra_trade'] and *[local-name() = 'Path' and text() = '{nodePath}']]"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationTreeNodeDetailResponse_INTRA_TRADE.xml"
                        )
                )
            );

        var client = factory.CreateClient();
        var response = await client.GetAsync(
            $"/classificationTrees/intra_trade/nodedetail?path={nodePath}",
            TestContext.Current.CancellationToken
        );
        var payload =
            await response.Content.ReadFromJsonAsync<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(),
            response.Content.Headers.ContentType?.MediaType
        );
        Assert.NotNull(payload);
        Assert.Equal("intra_trade", payload.TreeId);
        Assert.Equal(nodePath, payload.NodePath);
        Assert.NotNull(payload.Node);
        Assert.Null(payload.Node.CnCode);
        Assert.NotNull(payload.Node.CertificateModel);
        Assert.Equal(11978, payload.Node.CertificateModel.ModelId);
        Assert.Equal("11978", payload.Node.CertificateModel.ShortTitle);
        Assert.Equal("2022/497 (2021/403) Model animal health certificate for the movement between Member States of an individual equine animal not intended for slaughter (Model ‘EQUI-INTRA-IND’)", payload.Node.CertificateModel.LongTitle);
        Assert.Equal("2022/497 (2021/403) Model animal health certificate for the movement between Member States of an individual equine animal not intended for slaughter (Model ‘EQUI-INTRA-IND’)", payload.Node.CertificateModel.LongTitle);
        Assert.Equal(
            DateTimeOffset.Parse(
                "2022-12-07T18:04:10.000Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal
            ),
            payload.Node.CertificateModel.CreatedOn
        );
        Assert.Equal(
            DateTimeOffset.Parse(
                "2022-12-07T18:04:10.000Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal
            ),
            payload.Node.CertificateModel.UpdatedOn
        );
        Assert.True(payload.Node.Selectable);
        Assert.Equal("certificate", payload.Node.NodeType);
        AssertAttributeStringArray(
            payload.Attributes!,
            "AVAILABLE_EU_INTRA_DESCRIPTOR_COLUMNS",
            [
                "TAXON_ID",
                "ANIMAL_SUBCATEGORY",
                "GENDER",
                "IDENTIFICATION_SYSTEM",
                "IDENTIFICATION_NUMBER",
                "AGE",
                "QUANTITY",
            ]
        );
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_WithoutPathOrCnCode_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(
            "/classificationTrees/intra_trade/nodedetail",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static void AssertAttributeStringArray(
        IEnumerable<NodeAttribute> attributes,
        string key,
        string[] expectedValues
    )
    {
        Assert.Contains(
            attributes,
            attribute =>
                attribute.Key == key
                && attribute.Value is { } value
                && value.ValueKind == JsonValueKind.Array
                && value.EnumerateArray().Select(element => element.GetString()).SequenceEqual(expectedValues)
        );
    }
}
