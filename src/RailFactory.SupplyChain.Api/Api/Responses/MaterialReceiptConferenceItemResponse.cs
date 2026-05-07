namespace RailFactory.SupplyChain.Api.Api.Responses;

/// <summary>
/// Summary response for an item in a blind conference.
/// </summary>
public record MaterialReceiptConferenceItemResponse(
    Guid Id,
    string MaterialCode,
    string UnitOfMeasure,
    string? OriginalDescription,
    string? ImageUrl = null);
