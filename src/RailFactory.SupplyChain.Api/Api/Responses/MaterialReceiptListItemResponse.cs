using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.SupplyChain.Api.Api.Responses;

/// <summary>
/// Summary response for a material receipt in a list.
/// </summary>
public record MaterialReceiptListItemResponse(
    Guid Id,
    string ReceiptNumber,
    string DocumentNumber,
    string SupplierName,
    DateTime IssuedAt,
    string? AccessKey,
    decimal? TotalValue,
    DisplayStatus Status,
    int ItemCount);
