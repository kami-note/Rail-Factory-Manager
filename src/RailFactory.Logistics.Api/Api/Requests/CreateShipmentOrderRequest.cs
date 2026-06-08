namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record CreateShipmentOrderRequest(
    Guid? ProductionOrderRef,
    string? Notes,
    decimal? DeliveryLatitudeDeg = null,
    decimal? DeliveryLongitudeDeg = null,
    string? DeliveryCity = null,
    // Recipient (destinatário NF-e) — optional, required for automatic fiscal emission
    string? RecipientCnpj = null,
    string? RecipientName = null,
    string? RecipientEmail = null,
    string? RecipientStreet = null,
    string? RecipientNumber = null,
    string? RecipientDistrict = null,
    string? RecipientCity = null,
    string? RecipientState = null,
    string? RecipientZipCode = null,
    string? NatureOfOperation = null,
    string? RecipientIe = null,
    int ModalidadeFrete = 0);
