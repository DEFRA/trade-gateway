using System.Net;
using Api.Constants;
using Api.Contract;
using Refit;
using Trade.Gateway.Api.Contract.ReferenceData;
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
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationSectionsResponse.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>(),
            response.ContentHeaders?.ContentType?.MediaType
        );

        Assert.Equal(ReferenceDataService.ReferenceDataServiceV1, response.Content!.Service);
        Assert.Contains(
            response.Content.Sections!,
            section =>
                section
                    is {
                        ClassCode: "ACT",
                        Chapter: "veterinary",
                        Lms: true,
                        Description: "Animal act",
                        Active: true,
                        Scopes: ["EFTA", "EU"],
                        OperatorActivities: ["animal_act"]
                    }
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
        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
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
        var response = await client.GetClassificationSections(TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationTreeResponse_INTRA_TRADE.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetClassificationTree("intra_trade", TestContext.Current.CancellationToken);

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>(),
            response.ContentHeaders?.ContentType?.MediaType
        );
        await Verify(response.Content);

        // ensure that the certificates are correctly mapping - find cert with model id 11978
        IEnumerable<ClassificationTreeNode> Flatten(IEnumerable<ClassificationTreeNode>? nodes) =>
            (nodes ?? Enumerable.Empty<ClassificationTreeNode>()).SelectMany(n =>
                new[] { n }.Concat(Flatten(n.Children))
            );

        var certNode = Flatten(response.Content!.Nodes).FirstOrDefault(n => n.Certificate?.ModelId == 11978);

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
                Response
                    .Create()
                    .WithCallback(async _ =>
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
        var response = await client.GetClassificationTree(treeId, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
        var response = await client.GetClassificationTree(treeId, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetClassificationTree(treeId, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetClassificationTreeNodeDetailResponse_INTRA_TRADE.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetClassificationTreeNodeDetail(
            "intra_trade",
            nodeId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(),
            response.ContentHeaders?.ContentType?.MediaType
        );
        await Verify(response.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
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
        var response = await client.GetClassificationTreeNodeDetail(
            "intra_trade",
            nodeId,
            TestContext.Current.CancellationToken
        );
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetClassificationTreeNodeDetail(
            "intra_trade",
            nodeId,
            TestContext.Current.CancellationToken
        );
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
                        await SoapUtilities.CreateResponseFromResource(
                            HttpStatusCode.OK,
                            "Api.Tests.Samples.REFERENCE_DATA.GetMetadatasResponse_ACCOMPANYING_DOCUMENT_TYPE.xml"
                        )
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetMetadatas(metadataType, TestContext.Current.CancellationToken);

        Assert.Equal(
            MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>(),
            response.ContentHeaders?.ContentType?.MediaType
        );
        await Verify(response.Content);
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
        var response = await client.GetMetadatas(metadataType, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(async _ =>
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
        var response = await client.GetMetadatas(metadataType, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
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
                Response
                    .Create()
                    .WithCallback(_ =>
                        SoapUtilities.StubResponseMessage(HttpStatusCode.InternalServerError, SenderFault)
                    )
            );

        var client = await factory.CreateClientForPrincipalAsync("test-reference-data-reader");
        var response = await client.GetMetadatas(metadataType, TestContext.Current.CancellationToken);
        await Verify((response.Error as ValidationApiException)?.Content);
    }
}
