using Api.Contract;
using Api.Extensions;
using Api.Mapping;
using Api.Utils.Http;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;
using Trade.Gateway.Api.Contract.ReferenceData;

namespace Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static void UseReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("reference-data/classifications/sections", GetClassificationSections)
            .WithName("GetClassificationSections")
            .WithSummary("List classification sections.")
            .WithDescription("Returns every classification section with its groups and the sections each group holds.")
            .Produces<DefraUNVTDProfileClassificationSectionListResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>()
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("reference-data/classifications/trees/{treeId}", GetClassificationTree)
            .WithName("GetClassificationTree")
            .WithSummary("Fetch a classification tree.")
            .WithDescription("Returns the full node tree for one classification tree identifier.")
            .Produces<DefraUNVTDProfileClassificationTreeResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("reference-data/classifications/trees/{treeId}/nodes/{nodeId}", GetClassificationTreeNodeDetail)
            .WithName("GetClassificationTreeNodeDetail")
            .WithSummary("Fetch a node of a classification tree.")
            .WithDescription(
                "Returns one node's detail within the tree identified by treeId: attributes, "
                    + "legislation references and child nodes."
            )
            .Produces<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>()
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("reference-data/metadata/{metadataType}", GetMetadatas)
            .WithName("GetMetadata")
            .WithSummary("List reference metadata of one type.")
            .WithDescription(
                "Returns the metadata entries of the requested type, such as the qualifier and "
                    + "measurement lists used when composing certificate data."
            )
            .Produces<DefraUNVTDProfileMetadataListResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>()
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    /// <param name="referenceDataService">Reads the sections from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> GetClassificationSections(
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var classificationSections = await referenceDataService.GetClassificationSections(languageCode);

        if (classificationSections == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Classification sections for language '{languageCode}' not found."
            );
        }

        return Results.Json(
            ClassificationSectionMapper.Map(classificationSections),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>()
        );
    }

    /// <param name="treeId" example="intra_trade">Identifier of the classification tree to fetch.</param>
    /// <param name="referenceDataService">Reads the tree from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> GetClassificationTree(
        string treeId,
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var classificationTreeNodes = await referenceDataService.GetClassificationTree(treeId, languageCode);

        if (classificationTreeNodes == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Classification tree with id '{treeId}' not found."
            );
        }

        return Results.Json(
            ClassificationTreeMapper.Map(classificationTreeNodes, treeId),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
        );
    }

    /// <param name="treeId" example="intra_trade">Identifier of the classification tree the node belongs to.</param>
    /// <param name="nodeId" example="R_N-10000_N-10065_L-10121_L-10301_C-11978">Node path of the node to fetch, as returned by the tree response.</param>
    /// <param name="referenceDataService">Reads the node detail from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> GetClassificationTreeNodeDetail(
        string treeId,
        string nodeId,
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);
        var response = await referenceDataService.GetClassificationTreeNodeDetail(
            treeId,
            nodeId.ToNodePath(),
            languageCode
        );

        if (response == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Classification tree node detail with id '{treeId}' nodeId '{nodeId}' not found."
            );
        }

        return Results.Json(
            ClassificationTreeNodeDetailMapper.Map(response, treeId, nodeId),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>()
        );
    }

    /// <param name="metadataType" example="ACCOMPANYING_DOCUMENT_TYPE">Type of reference metadata list to return.</param>
    /// <param name="referenceDataService">Reads the metadata from TracesNT.</param>
    /// <param name="acceptLanguage">Preferred language for returned names, as a BCP 47 language tag.</param>
    private static async Task<IResult> GetMetadatas(
        string metadataType,
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var metadatas = await referenceDataService.GetMetadatas(metadataType, languageCode);

        if (metadatas == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"Metadata of type '{metadataType}' not found."
            );
        }

        return Results.Json(
            MetadataMapper.Map(metadatas, metadataType),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>()
        );
    }
}
