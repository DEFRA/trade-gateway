using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Api.Constants;
using Api.Contract;
using Trade.Gateway.Api.Contract.ReferenceData;
using Microsoft.AspNetCore.Mvc;
using WireMock.ResponseBuilders;

namespace Api.Tests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class ReferenceDataEndpointsTests(TradeGatewayWebApplicationFactory factory)
{
    private const string SenderFault = """
                               <?xml version='1.0' encoding='UTF-8'?>
                               <S:Envelope xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
                                 <S:Body>
                                   <S:Fault>
                                     <faultcode>S:Client</faultcode>
                                     <faultstring xml:lang="en">Bad request</faultstring>
                                   </S:Fault>
                                 </S:Body>
                               </S:Envelope>
                               """;


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

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync(
            "/reference-data/classifications/sections",
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
                section is { ClassCode: "ACT", Chapter: "veterinary", Lms: true, Description: "Animal act", Active: true, Scopes: ["EFTA", "EU"], OperatorActivities: ["animal_act"] }
        );
    }

    [Fact]
    public async Task GetClassificationSections_TracesCommunicationFailure_ReturnsBadGatewayProblem()
    {
        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationSections\"",
                    "/*[local-name() = 'GetClassificationSectionsRequest']"
                )
            )
            .RespondWith(Response.Create().WithStatusCode(500));

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync("/reference-data/classifications/sections", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal("Bad Gateway", problem.Title);
    }

    [Fact]
    public async Task GetClassificationSections_NoSections_ReturnsNotFoundProblem()
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
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            """
                            <?xml version='1.0' encoding='UTF-8'?>
                            <S:Envelope xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
                              <S:Body>
                                <ns13:GetClassificationSectionsResponse xmlns:ns13="http://ec.europa.eu/tracesnt/referencedata/v1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:nil="true" />
                              </S:Body>
                            </S:Envelope>
                            """
                        )
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync("/reference-data/classifications/sections", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Contains("Classification sections", problem.Detail ?? string.Empty);
        Assert.Contains("en", problem.Detail ?? string.Empty);
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

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync(
            "/reference-data/classifications/trees/intra_trade",
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

        // ensure that the certificates are correctly mapping - find cert with model id 11978
        IEnumerable<ClassificationTreeNode> Flatten(IEnumerable<ClassificationTreeNode>? nodes) =>
            (nodes ?? Enumerable.Empty<ClassificationTreeNode>())
                .SelectMany(n => new[] { n }.Concat(Flatten(n.Children)));

        var certNode = Flatten(payload.Nodes).FirstOrDefault(n => n.Certificate?.ModelId == 11978);

        Assert.NotNull(certNode);
        Assert.NotNull(certNode!.Certificate);
        Assert.Equal(11978, certNode.Certificate!.ModelId);
        Assert.Equal("11978", certNode.Certificate.ShortTitle);
        Assert.Contains("Model animal health certificate", certNode.Certificate.LongTitle ?? string.Empty);
        Assert.NotNull(certNode.Certificate.CreatedOn);
        Assert.Equal(TimeSpan.Zero, certNode.Certificate.CreatedOn?.Offset);
        Assert.NotNull(certNode.Certificate.UpdatedOn);
        Assert.Equal(TimeSpan.Zero, certNode.Certificate.UpdatedOn?.Offset);
    }

    [Fact]
    public async Task GetClassificationTree_UnknownTreeId_ReturnsNotFoundProblem()
    {
        const string treeId = "unknown_tree";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationTree\"",
                    $"/*[local-name() = 'GetClassificationTreeRequest']/*[local-name() = 'TreeID' and text() = '{treeId}']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            """
                            <?xml version='1.0' encoding='UTF-8'?>
                            <S:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/" xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
                              <env:Header/>
                              <S:Body>
                                <S:Fault xmlns:ns3="http://www.w3.org/2003/05/soap-envelope" xmlns="">
                                  <faultcode>S:Client</faultcode>
                                  <faultstring>Invalid classification tree ID</faultstring>
                                  <detail>
                                    <ns13:PermissionDeniedException xmlns:ns9="http://ec.europa.eu/tracesnt/referencedata/classificationtree/v1" xmlns:ns8="http://ec.europa.eu/tracesnt/referencedata/classificationsection/v1" xmlns:ns7="http://ec.europa.eu/tracesnt/referencedata/certificatemodel/v1" xmlns:ns6="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:ns5="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd" xmlns:ns4="http://ec.europa.eu/sanco/tracesnt/message/v1" xmlns:ns3="http://ec.europa.eu/sanco/tracesnt/error/v01" xmlns:ns2="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:ns16="urn:un:unece:uncefact:codelist:standard:ISO:ISO2AlphaLanguageCode:2006-10-27" xmlns:ns15="http://ec.europa.eu/tracesnt/body/v3" xmlns:ns14="http://www.w3.org/2000/09/xmldsig#" xmlns:ns12="http://ec.europa.eu/tracesnt/referencedata/laboratorytest/v1" xmlns:ns11="http://ec.europa.eu/tracesnt/referencedata/common/v1" xmlns:ns10="http://ec.europa.eu/tracesnt/referencedata/nodeattribute/v1" xmlns:ns13="http://ec.europa.eu/tracesnt/referencedata/v1"/>
                                  </detail>
                                </S:Fault>
                              </S:Body>
                            </S:Envelope>
                            """
                        )
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/classifications/trees/{treeId}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Contains(treeId, problem.Detail);
    }

    [Fact]
    public async Task GetClassificationTree_TracesCommunicationFailure_ReturnsBadGatewayProblem()
    {
        const string treeId = "intra_trade";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationTree\"",
                    $"/*[local-name() = 'GetClassificationTreeRequest']/*[local-name() = 'TreeID' and text() = '{treeId}']"
                )
            )
            .RespondWith(Response.Create().WithStatusCode(500));

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/classifications/trees/{treeId}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal("Bad Gateway", problem.Title);
    }

    [Fact]
    public async Task GetClassificationTree_SenderFault_ReturnsInternalServerErrorProblem()
    {
        const string treeId = "intra_trade";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getClassificationTree\"",
                    $"/*[local-name() = 'GetClassificationTreeRequest']/*[local-name() = 'TreeID' and text() = '{treeId}']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    _ => SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/classifications/trees/{treeId}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("An internal error occurred.", problem.Detail);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_ReturnsMappedResponse()
    {
        const string nodePath = "R/N-10000/N-10065/L-10121/L-10301/C-11978";
        const string nodeId = "R_N-10000_N-10065_L-10121_L-10301_C-11978";

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

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync(
            $"/reference-data/classifications/trees/intra_trade/nodes/{nodeId}",
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

        Assert.DoesNotContain(payload.Attributes!, a => a.Key == "SELECTABLE_DOCUMENT_LINKS");
        Assert.DoesNotContain(payload.Attributes!, a => a.Key.EndsWith("_CLASSIFICATION_SECTIONS"));
        Assert.DoesNotContain(payload.Attributes!, a => a.Key.StartsWith("TAXON_"));

        Assert.NotNull(payload.DocumentTypes);
        Assert.Contains(
            payload.DocumentTypes!,
            d =>
                d is { Key: "SELECTABLE_DOCUMENT_LINKS", DocumentLinkTypes.Count: 4 }
                && d.DocumentLinkTypes.Select(x => (x.DocumentType, x.LinkType)).SequenceEqual(
                    new[]
                    {
                        ("EU_INTRA", "ATTACHED_TO"),
                        ("ACCOMPANYING_DOCUMENT", "ATTACHED_TO"),
                        ("JOURNEY_LOG", "ATTACHED_TO"),
                        ("EU_EXPORT", "ATTACHED_TO"),
                    }
                )
        );

        Assert.NotNull(payload.Taxons);
        Assert.Contains(
            payload.Taxons!,
            t => t is { TaxonId: 142608, EppoCode: "1EQUCB", Name: "Equus cabalus", LanguageId: "la" }
        );

        Assert.Null(payload.InvasiveTaxons);

        Assert.NotNull(payload.ClassificationSectionGroups);
        Assert.Contains(
            payload.ClassificationSectionGroups!,
            g => g.Id == "CONSIGNEE_CLASSIFICATION_SECTIONS" && g.Sections is { Count: > 0 }
        );

        Assert.NotNull(payload.LegislationAttributes);
        Assert.NotEmpty(payload.LegislationAttributes!);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_NotFoundFromSoap_ReturnsNotFoundProblem()
    {
        const string nodePath = "R/N-10000/N-10065/L-10121/L-10301/C-11978";
        const string nodeId = "R_N-10000_N-10065_L-10121_L-10301_C-11978";

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
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            """
                            <?xml version='1.0' encoding='UTF-8'?>
                            <S:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/" xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
                              <env:Header/>
                              <S:Body>
                                <S:Fault xmlns:ns3="http://www.w3.org/2003/05/soap-envelope" xmlns="">
                                  <faultcode>S:Client</faultcode>
                                  <faultstring>Node not found</faultstring>
                                  <detail>
                                    <ns13:NodeNotFoundException xmlns:ns9="http://ec.europa.eu/tracesnt/referencedata/classificationtree/v1" xmlns:ns8="http://ec.europa.eu/tracesnt/referencedata/classificationsection/v1" xmlns:ns7="http://ec.europa.eu/tracesnt/referencedata/certificatemodel/v1" xmlns:ns6="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:ns5="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd" xmlns:ns4="http://ec.europa.eu/sanco/tracesnt/message/v1" xmlns:ns3="http://ec.europa.eu/sanco/tracesnt/error/v01" xmlns:ns2="http://ec.europa.eu/sanco/tracesnt/base/v4" xmlns:ns16="urn:un:unece:uncefact:codelist:standard:ISO:ISO2AlphaLanguageCode:2006-10-27" xmlns:ns15="http://ec.europa.eu/tracesnt/body/v3" xmlns:ns14="http://www.w3.org/2000/09/xmldsig#" xmlns:ns12="http://ec.europa.eu/tracesnt/referencedata/laboratorytest/v1" xmlns:ns11="http://ec.europa.eu/tracesnt/referencedata/common/v1" xmlns:ns10="http://ec.europa.eu/tracesnt/referencedata/nodeattribute/v1" xmlns:ns13="http://ec.europa.eu/tracesnt/referencedata/v1"/>
                                  </detail>
                                </S:Fault>
                              </S:Body>
                            </S:Envelope>
                            """
                        )
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync(
            $"/reference-data/classifications/trees/intra_trade/nodes/{nodeId}",
            TestContext.Current.CancellationToken
        );
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
    }

    [Fact]
    public async Task GetClassificationTreeNodeDetail_SenderFault_ReturnsInternalServerErrorProblem()
    {
        const string nodePath = "R/N-10000/N-10065/L-10121/L-10301/C-11978";
        const string nodeId = "R_N-10000_N-10065_L-10121_L-10301_C-11978";

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
                    _ => SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync(
            $"/reference-data/classifications/trees/intra_trade/nodes/{nodeId}",
            TestContext.Current.CancellationToken
        );
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("An internal error occurred.", problem.Detail);
    }

    [Fact]
    public async Task GetMetadatas_ReturnsMappedResponse()
    {
        const string metadataType = "ACCOMPANYING_DOCUMENT_TYPE";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getMetadatas\"",
                    $"/*[local-name() = 'GetMetadatasRequest']/*[local-name() = 'MetadataType' and text() = '{metadataType}']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetMetadatasResponse_ACCOMPANYING_DOCUMENT_TYPE.xml"
                        )
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/metadata/{metadataType}", TestContext.Current.CancellationToken);
        var payload =
            await response.Content.ReadFromJsonAsync<DefraUNVTDProfileMetadataListResponse>(
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>(),
            response.Content.Headers.ContentType?.MediaType
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(ReferenceDataSource.Traces, payload.Source);
        Assert.Equal(metadataType, payload.MetadataType);
        Assert.NotNull(payload.Items);
        Assert.Contains(
            payload.Items!,
            i => i is { Value: "AIRWAY_BILL", Active: true, MappedValue: null, DisplayName: "Air Waybill" }
        );
    }

    [Fact]
    public async Task GetMetadatas_TracesCommunicationFailure_ReturnsBadGatewayProblem()
    {
        const string metadataType = "ACCOMPANYING_DOCUMENT_TYPE";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getMetadatas\"",
                    $"/*[local-name() = 'GetMetadatasRequest']/*[local-name() = 'MetadataType' and text() = '{metadataType}']"
                )
            )
            .RespondWith(Response.Create().WithStatusCode(500));

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/metadata/{metadataType}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(502, problem.Status);
        Assert.Equal("Bad Gateway", problem.Title);
    }

    [Fact]
    public async Task GetMetadatas_NotFoundFromSoap_ReturnsNotFoundProblem()
    {
        const string metadataType = "ACCOMPANYING_DOCUMENT_TYPE";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getMetadatas\"",
                    $"/*[local-name() = 'GetMetadatasRequest']/*[local-name() = 'MetadataType' and text() = '{metadataType}']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    async _ =>
                        SoapUtilities.StubResponseMessage(
                            HttpStatusCode.OK,
                            """
                            <?xml version='1.0' encoding='UTF-8'?>
                            <S:Envelope xmlns:S="http://schemas.xmlsoap.org/soap/envelope/">
                              <S:Body>
                                <ns13:GetMetadatasResponse xmlns:ns13="http://ec.europa.eu/tracesnt/referencedata/v1" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:nil="true" />
                              </S:Body>
                            </S:Envelope>
                            """
                        )
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/metadata/{metadataType}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Contains(metadataType, problem.Detail);
    }

    [Fact]
    public async Task GetMetadatas_SenderFault_ReturnsInternalServerErrorProblem()
    {
        const string metadataType = "ACCOMPANYING_DOCUMENT_TYPE";

        factory.WireMockServer.Reset();
        factory
            .WireMockServer.Given(
                SoapUtilities.CreateSoapRequestInterceptor(
                    "\"getMetadatas\"",
                    $"/*[local-name() = 'GetMetadatasRequest']/*[local-name() = 'MetadataType' and text() = '{metadataType}']"
                )
            )
            .RespondWith(
                Response.Create().WithCallback(
                    _ => SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetAsync($"/reference-data/metadata/{metadataType}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal("An internal error occurred.", problem.Detail);
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
                && attribute.Value is { ValueKind: JsonValueKind.Array } value
                && value.EnumerateArray().Select(element => element.GetString()).SequenceEqual(expectedValues)
        );
    }
}
