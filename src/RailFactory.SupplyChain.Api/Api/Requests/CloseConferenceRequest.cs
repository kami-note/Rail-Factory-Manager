namespace RailFactory.SupplyChain.Api.Api.Requests;

/// <summary>
/// Request payload for closing a material receipt conference.
/// </summary>
/// <param name="Results">The list of counted items and their tracking data.</param>
public sealed record CloseConferenceRequest(IReadOnlyCollection<CountedResultRequest> Results);

/// <summary>
/// Represents the count result for a single item in a receipt.
/// </summary>
/// <param name="ItemId">The unique identifier of the receipt item.</param>
/// <param name="CountedQuantity">The physical quantity counted.</param>
/// <param name="ConfirmedLotNumber">Optional lot number confirmed by the operator.</param>
/// <param name="ConfirmedExpirationDate">Optional expiration date confirmed by the operator.</param>
public sealed record CountedResultRequest(
    Guid ItemId, 
    decimal CountedQuantity, 
    string? ConfirmedLotNumber, 
    DateTimeOffset? ConfirmedExpirationDate);
