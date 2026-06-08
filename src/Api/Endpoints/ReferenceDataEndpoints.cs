using System.ComponentModel;
using Api.Constants;
using Api.Contract;
using Api.Extensions;
using Api.Mapping;
using Api.Utils.Http;
using Defra.TradeGateway.Api.Contract.ReferenceData;
using Microsoft.AspNetCore.Mvc;
using TracesNT.Services;

namespace Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static void UseReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("classificationSections", GetClassificationSections)
            .Produces<DefraUNVTDProfileClassificationSectionListResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>()
            )
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("classificationTrees/{treeId}", GetClassificationTree)
            .Produces<DefraUNVTDProfileClassificationTreeResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
            )
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet(
                "classificationTrees/{treeId}/nodes/{nodeId}",
                GetClassificationTreeNodeDetail
            )
            .Produces<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>()
            )
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet(
                "metaDatas/{metadataType}",
                GetMetadatas
            )
            .Produces<DefraUNVTDProfileMetadataListResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>()
            )
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> GetClassificationSections(IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null)
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var classificationSections = await referenceDataService.GetClassificationSections(languageCode);

        if (classificationSections == null)
        {

            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: ResponseTitles.NotFound,
                detail: $"Classification sections for language '{languageCode}' not found."
            );
        }

        return Results.Json(
            ClassificationSectionMapper.Map(classificationSections),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>());
    }

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
                title: ResponseTitles.NotFound,
                detail: $"Classification tree with id '{treeId}' not found."
            );
        }

        return Results.Json(
                ClassificationTreeMapper.Map(classificationTreeNodes, treeId),
                    contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
                );
    }

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
                title: ResponseTitles.NotFound,
                detail: $"Classification tree node detail with id '{treeId}' nodeId '{nodeId}' not found."
            );
        }

        return Results.Json(ClassificationTreeNodeDetailMapper.Map(response, treeId, nodeId),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>()
        );
    }

    private static async Task<IResult> GetMetadatas(string metadataType, 
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
                title: ResponseTitles.NotFound,
                detail: $"Metadata of type '{metadataType}' not found."
            );
        }

        return Results.Json(
            MetadataMapper.Map(metadatas, metadataType),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>());
    }
}
