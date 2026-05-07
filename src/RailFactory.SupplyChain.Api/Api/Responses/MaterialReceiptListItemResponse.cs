namespace RailFactory.SupplyChain.Api.Api.Responses;

/// <summary>
/// Summary response for a material receipt in a list.
/// </summary>
public record MaterialReceiptListItemResponse(
    Guid Id,
    string ReceiptNumber,
    string DocumentNumber,
    string? AccessKey,
    decimal? TotalValue,
    DateOnly ReceiptDate,
    string Status,
    DateTimeOffset CreatedAt,
    int ItemCount);
