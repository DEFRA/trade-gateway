using System.ComponentModel;
using Api.Contract;
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
            );

        app.MapGet("classificationTrees/{classificationTreeId}", GetClassificationTree)
            .Produces<DefraUNVTDProfileClassificationTreeResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
            );

        app.MapGet(
                "classificationTrees/{classificationTreeId}/nodedetail",
                GetClassificationTreeNodeDetail
            )
            .Produces<DefraUNVTDProfileClassificationTreeNodeDetailResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeNodeDetailResponse>()
            );

        app.MapGet(
                "metaDatas/{metadataType}",
                GetMetadatas
            )
            .Produces<DefraUNVTDProfileMetadataListResponse>(
                200,
                MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>()
            );
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
                title: "Not Found",
                detail: $"Classification sections for language '{languageCode}' not found."
            );
        }

        return Results.Json(
            ClassificationSectionMapper.Map(classificationSections),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationSectionListResponse>());
    }

    private static async Task<IResult> GetClassificationTree(
        string classificationTreeId,
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var classificationTreeNodes = await referenceDataService.GetClassificationTree(classificationTreeId, languageCode);

        if (classificationTreeNodes == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Classification tree with id '{classificationTreeId}' not found."
            );
        }

        return Results.Json(
                ClassificationTreeMapper.Map(classificationTreeNodes, classificationTreeId),
                    contentType: MediaTypeAttribute.For<DefraUNVTDProfileClassificationTreeResponse>()
                );
    }

    private static async Task<IResult> GetClassificationTreeNodeDetail(
        [AsParameters] ClassificationTreeNodeDetailRequest request,
        IReferenceDataService referenceDataService,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null
    )
    {
        if (string.IsNullOrWhiteSpace(request.Path) && string.IsNullOrWhiteSpace(request.CnCode))
            return Results.BadRequest("Either path or cnCode is required.");

        var languageCode = AcceptLanguageParser.GetPrimaryLanguageCode(acceptLanguage);

        var response = await referenceDataService.GetClassificationTreeNodeDetail(
            request.TreeId,
            request.Path,
            request.CnCode,
            languageCode
        );

        if (response == null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Classification tree node detail with id '{request.TreeId}' path '{request.Path}' cnCode '{request.CnCode}' not found."
            );
        }

        return Results.Json(ClassificationTreeNodeDetailMapper.Map(response, request.TreeId!),
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
                title: "Not Found",
                detail: $"Metadata of type '{metadataType}' not found."
            );
        }

        return Results.Json(
            MetadataMapper.Map(metadatas, metadataType),
            contentType: MediaTypeAttribute.For<DefraUNVTDProfileMetadataListResponse>());
    }

    internal sealed record ClassificationTreeNodeDetailRequest
    {
        [FromRoute(Name = "classificationTreeId")]
        [Description("The Classification Tree Id, i.e. cheda.")]
        public required string TreeId { get; set; }

        [FromQuery(Name = "cnCode")]
        [Description("The CN code.")]
        public string? CnCode { get; set; }

        [FromQuery(Name = "path")]
        [Description("The classification tree node path.")]
        public string? Path { get; set; }
    }
}
