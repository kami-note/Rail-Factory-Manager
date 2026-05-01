using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Tenancy.Api.Domain;

public sealed record TenantRegisteredDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string TenantCode) : IDomainEvent;
